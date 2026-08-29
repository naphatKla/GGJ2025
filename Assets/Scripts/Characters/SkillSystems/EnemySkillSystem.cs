using System;
using System.Threading;
using Characters.Controllers;
using Characters.FeedbackSystems;
using Characters.SO.CharacterDataSO;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Characters.SkillSystems
{
    public class EnemySkillSystem : SkillSystem
    {
        private float skillNotifyDelay = 0.5f;
        
        // TODO: Delete this and implement system, this is for mockup test
        private bool _isCharging;
        private CancellationTokenSource cts = new();

        public override void AssignData(BaseController owner, BaseCharacterDataSo dataSO)
        {
            base.AssignData(owner, dataSO);
            if (dataSO is EnemyDataSo enemyDataSo)
                skillNotifyDelay = enemyDataSo.DelayBeforePerformSkill;
            else throw new FormatException();
        }

        public override async void PerformSkill(SkillType type)
        {
            if (!owner) return;
            
            if (type == SkillType.PrimarySkill)
            {
                if (_isCharging) return;
                var runtime = GetSkillRuntimeOrDefault(primarySkillData);
                if (!runtime) return;
                if (runtime.CurrentCooldown > skillNotifyDelay) return;
                // Checked before the notify tell as well, so the enemy doesn't wind up out of range.
                if (!CanPerformSkillNow(primarySkillData)) return;
                _isCharging = true;
                owner.FeedbackSystem.PlayFeedback(FeedbackName.Character.NotifySkill);
                await UniTask
                    .WaitForSeconds(skillNotifyDelay, cancellationToken: cts.Token)
                    .SuppressCancellationThrow();
                _isCharging = false;
            }
            
            if (cts.IsCancellationRequested) return;
            base.PerformSkill(type);
        }

        /// <summary>
        /// Enforces the skill's Activate Radius: an enemy may only trigger it while the player is inside
        /// that range. A radius of 0 means the skill has no range requirement.
        /// <para/>
        /// Re-checked here rather than only at decision time, so a skill with a notify delay cannot land
        /// after the player has already left its range.
        /// </summary>
        protected override bool CanPerformSkillNow(BaseSkillDataSo skillData)
        {
            if (!skillData || skillData.ActivateRadius <= 0f) return true;
            if (!owner) return false;

            var player = PlayerController.Instance;
            if (!player) return false;

            float sqrDistance = ((Vector2)(player.transform.position - owner.transform.position)).sqrMagnitude;
            return sqrDistance <= skillData.ActivateRadius * skillData.ActivateRadius;
        }

        public override void ResetSkillSystem()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = new CancellationTokenSource();
            base.ResetSkillSystem();
        }
    }
}
