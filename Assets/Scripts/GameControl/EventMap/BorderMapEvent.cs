using System;
using System.Collections.Generic;
using System.Threading;
using Characters.Controllers;
using Characters.SO.CharacterDataSO;
using Cysharp.Threading.Tasks;
using GameControl.Pattern;
using Manager;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Pool;

namespace GameControl.EventMap
{
    public class BorderMapEvent : BaseMapEvent
    {
        // ── Stat modifier ────────────────────────────────────────────────────
        /// <summary>
        ///     Direct flat-value overrides for border enemies.
        ///     Toggle the bool next to each stat to override it; unticked = keep prefab value.
        /// </summary>
        [Serializable]
        public struct BorderStatModifier
        {
            [HorizontalGroup("HP")] [LabelWidth(90)] [LabelText("Override HP")]
            public bool overrideHp;

            [HorizontalGroup("HP")] [ShowIf(nameof(overrideHp))] [LabelText("Max Health")]
            public float maxHealth;

            [HorizontalGroup("DMG")] [LabelWidth(90)] [LabelText("Override DMG")]
            public bool overrideDamage;

            [HorizontalGroup("DMG")] [ShowIf(nameof(overrideDamage))] [LabelText("Damage")]
            public float damage;

            [HorizontalGroup("SPD")] [LabelWidth(90)] [LabelText("Override SPD")]
            public bool overrideSpeed;

            [HorizontalGroup("SPD")] [ShowIf(nameof(overrideSpeed))] [LabelText("Move Speed")]
            public float moveSpeed;
            
            public static float ToPercent(bool active, float targetValue, float baseValue)
            {
                if (!active) return float.NaN;
                if (baseValue == 0f) return float.NaN;
                return (targetValue / baseValue - 1f) * 100f;
            }
        }
        
        [Header("Border Settings")] public EnemyController enemyPrefab;
        public BaseSpawnPattern patternType;
        public Transform spawnParent;
        public int enemyAmount = 20;

        [Header("Stat Modifier (optional)")] public BorderStatModifier statModifier;

        [Header("Behaviour")] [Tooltip("Enemies cannot be killed while the border is active.")]
        public bool godMode = true;

        [Tooltip("Enemies walk toward the spawn-center instead of standing still.")]
        public bool walkToCenter;

        [ShowIf(nameof(walkToCenter))] [Tooltip("Units per second used when walking to center.")]
        public float walkSpeed = 3f;

        [ShowIf(nameof(walkToCenter))] [Tooltip("Distance from center at which the enemy stops walking.")]
        public float walkStopRadius = 0.15f;

        public HitboxType HitboxType { get; }
        public float Radius { get; set; }
        public Vector3 Offset { get; set; }

        private ObjectPool<EnemyController> _borderPool;
        private readonly List<EnemyController> _spawnedEnemies = new();
        private Vector3 _centerPosition;

        private void Awake()
        {
            if (enemyPrefab != null)
                InitPool();
        }

        private void OnDestroy()
        {
            ReleaseAllBorderEnemies();
            _borderPool?.Dispose();
        }

        private void InitPool()
        {
            _borderPool = new ObjectPool<EnemyController>(
                CreateBorderEnemy,
                OnGetBorderEnemy,
                OnReleaseBorderEnemy,
                e =>
                {
                    if (e != null) Destroy(e.gameObject);
                },
                false,
                Mathf.Max(1, prewarmCount)
            );

            var prewarm = new List<EnemyController>(prewarmCount);
            for (var i = 0; i < prewarmCount; i++)
                prewarm.Add(_borderPool.Get());
            foreach (var e in prewarm)
                _borderPool.Release(e);
        }

        private EnemyController CreateBorderEnemy()
        {
            var parent = spawnParent != null ? spawnParent : transform;
            var obj = Instantiate(enemyPrefab.gameObject, parent);
            var ctrl = obj.GetComponent<EnemyController>();

            var anyOverride = statModifier.overrideHp
                              || statModifier.overrideDamage
                              || statModifier.overrideSpeed;

            if (anyOverride && ctrl.CharacterData is EnemyDataSo enemyData)
            {
                var hpPct = BorderStatModifier.ToPercent(statModifier.overrideHp, statModifier.maxHealth,
                    enemyData.MaxHealth);
                var dmgPct = BorderStatModifier.ToPercent(statModifier.overrideDamage, statModifier.damage,
                    enemyData.BaseDamage);
                var spdPct = BorderStatModifier.ToPercent(statModifier.overrideSpeed, statModifier.moveSpeed,
                    enemyData.BaseSpeed);
                ctrl.AssignCharacterData(enemyData.CopyInstance(hpPct, dmgPct, spdPct));
            }
            else
            {
                ctrl.AssignCharacterData(ctrl.CharacterData);
            }

            return ctrl;
        }

        private void OnGetBorderEnemy(EnemyController e)
        {
            e.gameObject.SetActive(true);
            e.ResetAllDependentBehavior();

            if (e.InputSystem != null) e.InputSystem.Enable = false;
            e.MovementSystem?.StopAllMovementAndTween();
            if (godMode) e.HealthSystem.SetInvincible(true);
        }

        private void OnReleaseBorderEnemy(EnemyController e)
        {
            if (e == null || e.gameObject == null) return;

            if (e.InputSystem != null) e.InputSystem.Enable = true;
            e.MovementSystem?.ResetMovementSystem();
            if (godMode) e.HealthSystem.SetInvincible(false);
            e.gameObject.SetActive(false);
        }
        
        public override async UniTask PlayPreview()
        {
            notifyFeedback?.PlayFeedbacks();
            previewEffect?.Play(true);

            await UniTask.Delay(
                TimeSpan.FromSeconds(Mathf.Max(0f, previewDuration)),
                DelayType.DeltaTime,
                PlayerLoopTiming.Update,
                destroyCancellationToken);
        }

        protected override void Perform()
        {
            if (enemyPrefab == null)
            {
                if (debug) Debug.LogWarning("[BorderMapEvent] enemyPrefab is null.");
                return;
            }

            if (_borderPool == null)
                InitPool();

            _centerPosition = PlayerController.Instance != null
                ? PlayerController.Instance.transform.position
                : Vector3.zero;

            SpawnBorderEnemies();
            DespawnAfterDuration(deletetime, _playToken).Forget();
        }

        // ── Spawn ────────────────────────────────────────────────────────────
        private void SpawnBorderEnemies()
        {
            if (patternType == null)
            {
                if (debug) Debug.LogWarning("[BorderMapEvent] patternType is null.");
                return;
            }

            var rows = patternType.CalculateRows(_centerPosition, enemyAmount);
            if (rows == null) return;

            foreach (var row in rows)
            foreach (var pos in row)
                SpawnSingleEnemy(pos);

            if (debug)
                Debug.Log($"[BorderMapEvent] Spawned {_spawnedEnemies.Count} border enemies.");
        }

        private void SpawnSingleEnemy(Vector2 pos)
        {
            var enemy = _borderPool.Get();
            enemy.transform.SetParent(spawnParent != null ? spawnParent : transform);
            enemy.transform.position = pos;
            enemy.CountedByMax = false;

            _spawnedEnemies.Add(enemy);

            if (walkToCenter)
                WalkEnemyToCenter(enemy, _centerPosition, _playToken).Forget();
        }
        private async UniTaskVoid WalkEnemyToCenter(EnemyController enemy, Vector3 center,
            CancellationToken token)
        {
            if (enemy == null) return;

            try
            {
                while (enemy != null && enemy.gameObject.activeInHierarchy)
                {
                    token.ThrowIfCancellationRequested();

                    var dist = Vector3.Distance(enemy.transform.position, center);
                    if (dist <= walkStopRadius) break;

                    enemy.transform.position = Vector3.MoveTowards(
                        enemy.transform.position,
                        center,
                        walkSpeed * Time.deltaTime);

                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        
        private async UniTaskVoid DespawnAfterDuration(float duration, CancellationToken token)
        {
            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(Mathf.Max(0f, duration)),
                    DelayType.DeltaTime,
                    PlayerLoopTiming.Update,
                    token);
            }
            catch (OperationCanceledException)
            {
                if (debug) Debug.Log("[BorderMapEvent] Despawn timer cancelled.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                ReleaseAllBorderEnemies();
            }
        }

        private void ReleaseAllBorderEnemies()
        {
            if (_borderPool == null) return;

            foreach (var enemy in _spawnedEnemies.ToArray())
            {
                if (enemy == null || enemy.gameObject == null) continue;
                _borderPool.Release(enemy);
            }

            _spawnedEnemies.Clear();

            if (debug) Debug.Log("[BorderMapEvent] All border enemies released.");
        }
        
        private void OnDrawGizmos()
        {
            if (patternType == null) return;
            
            int enemyCount = Mathf.Max(1, enemyAmount);
            Gizmos.color = Color.red;
            Vector2 center = (Vector2)transform.position;
            var positions = patternType.CalculatePositions(center, enemyCount);
            foreach (var pos in positions)
            {
                Gizmos.DrawWireSphere(pos, 1f);
            }
        }
    }
}