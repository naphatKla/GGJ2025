using System;
using System.Threading;
using Characters.FeedbackSystems;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using GlobalSettings;
using Manager;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    public class SkillPressureRuntime : BaseSkillRuntime<SkillPressureDataSo>
    {
        protected override void OnSkillStart()
        {
            if (skillData.StopOnCharge)
                owner.MovementSystem.StopAllMovementAndTween();
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            try
            {
                await UniTask.WaitForSeconds(skillData.ChargeDuration, cancellationToken: cancelToken);
            }
            catch (Exception e)
            {       
               
            }
            
            if (cancelToken.IsCancellationRequested) return;
            if (owner.HealthSystem.IsDead) return;
            
            var layerMask = CharacterGlobalSettings.Instance.EnemyLayerDictionary[transform.tag];
            var targets = Physics2D.OverlapCircleAll(transform.position, skillData.ExplosionRadius, layerMask);

            owner.TryPlayFeedback(skillData.BombFeedback);
            foreach (var target in targets)
            {
                CombatManager.ApplyCalculatedDamageTo(target.gameObject, owner.gameObject,owner.CharacterData.CharacterId , owner.gameObject,
                    target.ClosestPoint(owner.transform.position), skillData.BaseDamage, skillData.DamageMultiplier, 0,
                    0, 0, 0);
            }
        }

        protected override void OnSkillExit()
        {
            if (!owner) return;
            if (skillData.StopOnCharge)
                owner.MovementSystem.ResetMovementSystem();
            
            owner.HealthSystem.ForceDead();
        }

        private void OnDisable()
        {
            CancelSkill();
        }
    }
}