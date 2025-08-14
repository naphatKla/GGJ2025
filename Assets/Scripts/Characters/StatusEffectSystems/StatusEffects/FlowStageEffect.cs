using Characters.Controllers;
using Characters.SO.StatusEffectSO;
using UnityEngine;

namespace Characters.StatusEffectSystems.StatusEffects
{
    public class FlowStageEffect : BaseStatusEffect<FlowStageEffectDataSo>
    {
        public override void OnStart(BaseController owner)
        {
            Debug.Log("enter flow stage");
            owner.CombatSystem.AddCurrentDamage(effectData.DamageIncrease);
            owner.CombatSystem.AddCurrentDamageMultiplierPercent(effectData.DamagePercentIncrease);
            owner.MovementSystem.AddCurrentSpeedMultiplier(effectData.SpeedPercentIncrease);
        }

        public override void OnUpdate(BaseController owner, float deltaTime)
        {
            
        }

        public override void OnExit(BaseController owner)
        {
            Debug.Log("exit flow stage");
            owner.CombatSystem.AddCurrentDamage(-effectData.DamageIncrease);
            owner.CombatSystem.AddCurrentDamageMultiplierPercent(-effectData.DamagePercentIncrease);
            owner.MovementSystem.AddCurrentSpeedMultiplier(-effectData.SpeedPercentIncrease);
        }
    }
}
