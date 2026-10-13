using System;
using System.Collections.Generic;
using System.Threading;
using Characters.Controllers;
using Characters.SkillSystems.SkillObjects;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using GlobalSettings;
using Manager;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    public class SkillPlasmaTagRuntime : BaseSkillRuntime<SkillPlasmaTagDataSo>
    {
        private const float InitialTargetRange = 5f;

        private readonly Collider2D[] _candidates = new Collider2D[64];

        // Targets that currently carry a bomb (entries are removed when that bomb finishes).
        private readonly HashSet<BaseController> _taggedTargets = new();

        // Bombs currently alive (counting down or exploding).
        private readonly List<PlasmaTagSkillObject> _activeBombs = new();

        private Action<GameObject> _onOwnerHitHandler;

        // ======================== SKILL LIFECYCLE ========================

        public override void AssignSkillData(BaseSkillDataSo skillData, BaseController owner)
        {
            base.AssignSkillData(skillData, owner);

            PoolingManager.Instance.Create<PlasmaTagSkillObject>(
                this.skillData.PlasmaTagSkillObject.name,
                PoolingGroupName.SkillObject,
                CreatePoolInstance,
                prewarmCount: this.skillData.MaxActiveBombs);

            _onOwnerHitHandler = target =>
            {
                if (!IsPerforming) return;
                PlaceBombOnTarget(target).Forget();
            };
            owner.DamageOnTouch.OnHit += _onOwnerHitHandler;
        }

        protected override void OnSkillStart()
        {
            // _taggedTargets is NOT cleared here: each entry removes itself when its bomb
            // finishes, so clearing would let old and new bombs fight over the same entry.
            Transform firstTarget = FindClosestEnemy(owner.transform.position);
            if (firstTarget != null)
                PlaceBombOnTarget(firstTarget.gameObject).Forget();
        }

        protected override UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            return UniTask.WaitForSeconds(skillData.TagDuration, cancellationToken: cancelToken);
        }

        protected override void OnSkillExit() { }

        private void OnDestroy()
        {
            if (owner == null) return;

            owner.DamageOnTouch.OnHit -= _onOwnerHitHandler;

            // Detonate everything first, clear the pool only after the explosions finish.
            // Static + captured args: this runtime is gone by the time the task completes.
            DetonateAllThenClearPool(
                skillData.PlasmaTagSkillObject.name,
                _activeBombs.ToArray()).Forget();
        }

        private static async UniTaskVoid DetonateAllThenClearPool(string poolName, PlasmaTagSkillObject[] bombs)
        {
            foreach (var bomb in bombs)
                if (bomb) bomb.ForceDetonate();

            // A bomb is done once it is idle again (reset by PlaceBombOnTarget's finally) or destroyed.
            await UniTask.WaitUntil(() => AllBombsFinished(bombs));

            PoolingManager.Current?.ClearPool(poolName);
        }

        private static bool AllBombsFinished(PlasmaTagSkillObject[] bombs)
        {
            foreach (var bomb in bombs)
                if (bomb && bomb.IsBusy) return false;
            return true;
        }

        // ======================== PLACE BOMB ========================

        private async UniTaskVoid PlaceBombOnTarget(GameObject targetGameObject)
        {
            // --- Validate ---
            if (!CombatManager.TryGetCharacterFromCache(targetGameObject, out var target)) return;
            if (target.HealthSystem == null || !target.HealthSystem.CanAim) return;
            if (_activeBombs.Count >= skillData.MaxActiveBombs) return;
            if (!_taggedTargets.Add(target)) return;

            // --- Capture everything before the async part (runtime may be destroyed meanwhile) ---
            string poolName = skillData.PlasmaTagSkillObject.name;
            PlasmaBombConfig config = BuildBombConfig();
            var tagged = _taggedTargets;
            var active = _activeBombs;

            // --- Spawn ---
            var bomb = SpawnBomb(poolName, target.transform.position);
            active.Add(bomb);

            // --- Run: countdown -> explosion -> cleanup (or earlier detonation if the target dies) ---
            try
            {
                await bomb.RunAsync(target, config);
            }
            catch (OperationCanceledException) { }
            finally
            {
                tagged.Remove(target);
                active.Remove(bomb);
                ReleaseBomb(poolName, bomb);
            }
        }

        private PlasmaBombConfig BuildBombConfig()
        {
            return new PlasmaBombConfig(
                ownerGo: owner.gameObject,
                ownerId: owner.CharacterData.CharacterId,
                countdown: skillData.BombCountdown,
                damageDuration: skillData.DamageActiveDuration,
                cleanupDelay: skillData.CleanupDelay,
                baseDamage: skillData.BaseDamage,
                damageMultiplier: skillData.DamageMultiplier,
                explodeOnTargetDead: skillData.ExplodeOnDead);
        }

        private static PlasmaTagSkillObject SpawnBomb(string poolName, Vector3 position)
        {
            var bomb = PoolingManager.Instance.Get<PlasmaTagSkillObject>(poolName);
            bomb.transform.position = position;
            bomb.ResetForPool();
            bomb.gameObject.SetActive(true);
            return bomb;
        }

        private static void ReleaseBomb(string poolName, PlasmaTagSkillObject bomb)
        {
            if (!bomb) return;

            bomb.ResetForPool();
            bomb.gameObject.SetActive(false);
            PoolingManager.Current?.Release(poolName, bomb);
        }

        // ======================== TARGETING ========================

        private Transform FindClosestEnemy(Vector2 origin)
        {
            LayerMask damageLayer = CharacterGlobalSettings.Instance.EnemyLayerDictionary[owner.tag];
            int found = Physics2D.OverlapCircleNonAlloc(origin, InitialTargetRange, _candidates, damageLayer);

            if (found <= 0) return null;

            Transform best = null;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < found; i++)
            {
                var col = _candidates[i];
                if (!col) continue;
                if (!CombatManager.TryGetCharacterFromCache(col.gameObject, out var t)) continue;
                if (!t.HealthSystem.CanAim) continue;
                if (t.transform == owner.transform) continue;

                float sqr = ((Vector2)t.transform.position - origin).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = t.transform;
                }
            }

            return best;
        }

        // ======================== POOL ========================

        private PlasmaTagSkillObject CreatePoolInstance()
        {
            PlasmaTagSkillObject obj = Instantiate(skillData.PlasmaTagSkillObject);
            obj.gameObject.SetActive(false);
            obj.transform.position = owner.transform.position;
            return obj;
        }
    }
}