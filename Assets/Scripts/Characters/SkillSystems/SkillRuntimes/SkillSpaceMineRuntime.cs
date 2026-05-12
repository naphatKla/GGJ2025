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
    public class SkillSpaceMineRuntime : BaseSkillRuntime<SkillSpaceMineDataSo>
    {
        private readonly Collider2D[] _candidates = new Collider2D[64];
        private readonly HashSet<BaseController> _taggedTargets = new();

        private System.Action<GameObject> _onOwnerHitHandler;

        public override void AssignSkillData(BaseSkillDataSo skillData, BaseController owner)
        {
            base.AssignSkillData(skillData, owner);
            PoolingManager.Instance.Create<SpaceMineSkillObject>(
                this.skillData.SpaceMineSkillObject.name,
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

        // ======================== LIFECYCLE ========================

        protected override void OnSkillStart()
        {
            _taggedTargets.Clear();

            // Hit แรกที่ trigger skill → IsPerforming ยัง false ตอน OnHit fire
            // แปะระเบิดตัวแรกเอง
            Transform firstTarget = FindClosestEnemy(owner.transform.position);
            if (firstTarget != null)
            {
                PlaceBombOnTarget(firstTarget.gameObject).Forget();
            }
        }

        protected override UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            return UniTask.WaitForSeconds(skillData.TagDuration, cancellationToken: cancelToken);
        }

        protected override void OnSkillExit()
        {
            // IsPerforming = false → _onOwnerHitHandler จะ return เอง
            // ไม่ clear _taggedTargets ที่นี่ เพราะ bomb ที่แปะไปแล้วยังทำงานอยู่
        }

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

            // ศัตรูตัวนี้ติดระเบิดอยู่แล้ว → ข้าม
            if (!_taggedTargets.Add(target)) return;

            var bomb = PoolingManager.Instance.Get<SpaceMineSkillObject>(skillData.SpaceMineSkillObject.name);
            bomb.transform.position = target.transform.position;
            bomb.ResetForPool();
            bomb.gameObject.SetActive(true);
            bomb.FollowTarget(target);

            // ─── Phase 1: Countdown ───
            await bomb.WaitCountdownAsync(skillData.BombCountdown, destroyCancellationToken);

            if (!bomb || !bomb.gameObject.activeSelf)
            {
                FinalizeBomb(bomb, target);
                return;
            }

            // ─── Phase 2: Enable damage for a limited time ───
            bool hasHit = false;

            bomb.DamageOnTouch.EnableDamage(
                owner.gameObject, owner.CharacterData.CharacterId,
                this, 1, skillData.BaseDamage, skillData.DamageMultiplier);

            bomb.DamageOnTouch.OnHit -= OnBombHit;
            bomb.DamageOnTouch.OnHit += OnBombHit;

            // รอ damageActiveDuration → ปิด damage ไม่ว่าจะโดนหรือไม่
            await UniTask.WaitForSeconds(skillData.DamageActiveDuration,
                cancellationToken: destroyCancellationToken);

            // Unsubscribe เผื่อไม่ได้โดนอะไรเลย
            bomb.DamageOnTouch.OnHit -= OnBombHit;
            bomb.DamageOnTouch.DisableDamage(this);

            // ─── Phase 3: VFX delay → cleanup ───
            if (skillData.CleanupDelay > 0f)
            {
                await UniTask.WaitForSeconds(skillData.CleanupDelay,
                    cancellationToken: destroyCancellationToken);
            }

            FinalizeBomb(bomb, target);
            return;

            // ─── Local: on-hit handler ───
            void OnBombHit(GameObject _)
            {
                if (hasHit) return;
                hasHit = true;
                // ไม่ cleanup ทันที — ให้ DamageOnTouch วน hit AOE ครบก่อน
                // damage จะถูกปิดโดย flow ด้านบนตอน damageActiveDuration หมด
            }
        }

        // ======================== CLEANUP ========================

        private void FinalizeBomb(SpaceMineSkillObject bomb, BaseController target)
        {
            _taggedTargets.Remove(target);

            if (!bomb) return;
            bomb.ResetForPool();
            bomb.gameObject.SetActive(false);
            PoolingManager.Current?.Release(skillData.SpaceMineSkillObject.name, bomb);
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

        private SpaceMineSkillObject CreatePoolInstance()
        {
            SpaceMineSkillObject skillObj = Instantiate(skillData.SpaceMineSkillObject);
            skillObj.gameObject.SetActive(false);
            skillObj.transform.position = owner.transform.position;
            return skillObj;
        }
    }
}