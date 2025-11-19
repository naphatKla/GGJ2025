using Cameras;
using Characters.Controllers;
using Characters.SO.StatusEffectSO;

namespace Characters.StatusEffectSystems.StatusEffects
{
    public class FlowStateEffect : BaseStatusEffect<FlowStateEffectDataSo>
    {
        public override void OnStart(BaseController owner)
        {
            owner.CombatSystem.AddCurrentDamage(effectData.DamageIncrease);
            owner.CombatSystem.AddCurrentDamageMultiplierPercent(effectData.DamagePercentIncrease);
            owner.MovementSystem.AddCurrentSpeedModifierPercentage(effectData.SpeedPercentIncrease);

      
            if (owner is not PlayerController player) return;
            player.FlowStateController.AddFlowScoreMultiplier(effectData.FlowScorePercentageAdded/100);
            
            if (!effectData.ExpandCamera) return;
            Cinemachine2DCameraController.Instance.PushOrtho(
                Cinemachine2DCameraController.Instance.defaultOrthoSize + effectData.AdditionalExpandSize, duration, this, 0.25f);
        }

        public override void OnUpdate(BaseController owner, float deltaTime)
        {
            
        }

        public override void OnExit(BaseController owner)
        {
            owner.CombatSystem.AddCurrentDamage(-effectData.DamageIncrease);
            owner.CombatSystem.AddCurrentDamageMultiplierPercent(-effectData.DamagePercentIncrease);
            owner.MovementSystem.AddCurrentSpeedModifierPercentage(-effectData.SpeedPercentIncrease);
            
            if (owner is not PlayerController player) return;
            player.FlowStateController.AddFlowScoreMultiplier(-effectData.FlowScorePercentageAdded/100);
            
            if (!effectData.ExpandCamera) return;
            if (!Cinemachine2DCameraController.Current) return;
            Cinemachine2DCameraController.Current.CancelByOwner(this);
        }
    }
}
