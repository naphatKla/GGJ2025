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
        
        public override void AssignSkillData(BaseSkillDataSo skillData, BaseController owner)
        {
            base.AssignSkillData(skillData, owner);
            PoolingManager.Instance.Create<SpaceMineSkillObject>(this.skillData.SpaceMineSkillObject.name, PoolingGroupName.SkillObject, CreatePoolInstance, prewarmCount: 2);
        }

        protected override void OnSkillStart()
        {
            PlaceBomb().Forget();
        }

        protected override UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            return UniTask.CompletedTask;
        }

        protected override void OnSkillExit()
        {
       
        }

        private SpaceMineSkillObject CreatePoolInstance()
        {
            SpaceMineSkillObject skillObj = Instantiate(skillData.SpaceMineSkillObject);
            skillObj.gameObject.SetActive(false);
            skillObj.transform.position = owner.transform.position;
            return skillObj;
        }

        private async UniTask PlaceBomb()
        {
            var bomb = PoolingManager.Instance.Get<SpaceMineSkillObject>(skillData.SpaceMineSkillObject.name);
            bomb.transform.position = owner.transform.position;
            bomb.gameObject.SetActive(true);
            bomb.FollowTarget(FindClosestEnemy(owner.transform.position));
            await bomb.WaitPlaceBombAsync();

            bool hasHit = false;

            bomb.DamageOnTouch.EnableDamage(
                owner.gameObject, owner.CharacterData.CharacterId,
                this, 1, skillData.BaseDamage, skillData.DamageMultiplier);

            bomb.DamageOnTouch.OnHit -= OnBombHit;
            bomb.DamageOnTouch.OnHit += OnBombHit;

            void OnBombHit(GameObject _)
            {
                if (hasHit) return; // ให้ cleanup ทำแค่ครั้งเดียว แต่ damage ยังวิ่งครบทุกตัว
                hasHit = true;
                CleanupBombDeferred(bomb).Forget();
            }
        }

        private async UniTaskVoid CleanupBombDeferred(SpaceMineSkillObject bomb)
        {
            // รอจบ frame ให้ DamageOnTouch วน hit ครบทุกตัวก่อน
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            bomb.DamageOnTouch.DisableDamage(this);
            bomb.StopFollow();
            bomb.gameObject.SetActive(false);
            PoolingManager.Current?.Release(skillData.SpaceMineSkillObject.name, bomb);
        }
        
        private BaseController FindClosestEnemy(Vector2 origin)
        {
            LayerMask damageLayer = CharacterGlobalSettings.Instance.EnemyLayerDictionary[owner.tag];
            int found = Physics2D.OverlapCircleNonAlloc(
                origin, 2, _candidates, damageLayer);

            if (found <= 0) return null;

            BaseController best = null;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < found; i++)
            {
                var col = _candidates[i];
                if (!col) continue;
                if (!CombatManager.TryGetCharacterFromCache(col.gameObject, out var target)) continue;

                var health = target.HealthSystem;
                if (!health.CanAim) continue;
                if (health.transform == owner.transform) continue;

                float sqr = ((Vector2)health.transform.position - origin).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = target;
                }
            }

            return best;
        }
    }
}
