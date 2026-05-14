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
        private readonly Collider2D[] _candidates = new Collider2D[64];
        private readonly HashSet<BaseController> _taggedTargets = new();

        private System.Action<GameObject> _onOwnerHitHandler;

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
            _taggedTargets.Clear();

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
            if (owner != null)
                owner.DamageOnTouch.OnHit -= _onOwnerHitHandler;
        }

        // ======================== PLACE BOMB ========================

        private async UniTaskVoid PlaceBombOnTarget(GameObject targetGameObject)
        {
            if (!CombatManager.TryGetCharacterFromCache(targetGameObject, out var target)) return;
            if (target.HealthSystem == null || !target.HealthSystem.CanAim) return;
            if (!_taggedTargets.Add(target)) return;

            // Capture ทุกอย่างเป็น local ก่อน async
            string poolName    = skillData.PlasmaTagSkillObject.name;
            float countdownDur = skillData.BombCountdown;
            float dmgActiveDur = skillData.DamageActiveDuration;
            float cleanupDel   = skillData.CleanupDelay;
            float baseDmg      = skillData.BaseDamage;
            float dmgMult      = skillData.DamageMultiplier;
            GameObject ownerGo = owner.gameObject;
            string ownerId     = owner.CharacterData.CharacterId;

            var bomb = PoolingManager.Instance.Get<PlasmaTagSkillObject>(poolName);
            bomb.transform.position = target.transform.position;
            bomb.ResetForPool();
            bomb.gameObject.SetActive(true);
            bomb.FollowTarget(target);

            var ct = bomb.destroyCancellationToken;

            try
            {
                // Phase 1: Countdown
                await bomb.WaitCountdownAsync(countdownDur, ct);

                if (!bomb || !bomb.gameObject.activeSelf) return;

                // Phase 2: Damage — ใช้ bomb เป็น source key
                bomb.DamageOnTouch.EnableDamage(ownerGo, ownerId, bomb, 1, baseDmg, dmgMult);

                await UniTask.WaitForSeconds(dmgActiveDur, cancellationToken: ct);

                if (!bomb || !bomb.gameObject.activeSelf) return;

                bomb.DamageOnTouch.DisableDamage(bomb);

                // Phase 3: VFX delay
                if (cleanupDel > 0f)
                    await UniTask.WaitForSeconds(cleanupDel, cancellationToken: ct);
            }
            catch (System.OperationCanceledException) { }
            finally
            {
                _taggedTargets.Remove(target);

                if (bomb)
                {
                    bomb.ResetForPool();
                    bomb.gameObject.SetActive(false);
                    PoolingManager.Current?.Release(poolName, bomb);
                }
            }
        }

        // ======================== TARGETING ========================

        private Transform FindClosestEnemy(Vector2 origin)
        {
            LayerMask damageLayer = CharacterGlobalSettings.Instance.EnemyLayerDictionary[owner.tag];
            int found = Physics2D.OverlapCircleNonAlloc(origin, 5f, _candidates, damageLayer);

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