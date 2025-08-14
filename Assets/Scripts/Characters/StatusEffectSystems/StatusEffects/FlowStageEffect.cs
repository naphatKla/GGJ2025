using Characters.Controllers;
using Characters.SO.StatusEffectSO;
using UnityEngine;

namespace Characters.StatusEffectSystems.StatusEffects
{
    public class FlowStageEffect : BaseStatusEffect<FlowStageEffectDataSo>
    {
        public override void OnStart(BaseController owner)
        {
            owner.CombatSystem.AddCurrentDamage(effectData.DamageIncrease);
            owner.CombatSystem.AddCurrentDamageMultiplierPercent(effectData.DamagePercentIncrease);
            owner.MovementSystem.AddCurrentSpeedMultiplier(effectData.SpeedPercentIncrease);
        }

        public override void OnUpdate(BaseController owner, float deltaTime)
        {
            
        }

        public override void OnExit(BaseController owner)
        {
            owner.CombatSystem.AddCurrentDamage(-effectData.DamageIncrease);
            owner.CombatSystem.AddCurrentDamageMultiplierPercent(-effectData.DamagePercentIncrease);
            owner.MovementSystem.AddCurrentSpeedMultiplier(-effectData.SpeedPercentIncrease);
        }
    }
}
