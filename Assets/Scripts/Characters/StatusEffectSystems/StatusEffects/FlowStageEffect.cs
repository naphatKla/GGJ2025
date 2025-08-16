using Characters.Controllers;
using Characters.SO.StatusEffectSO;

namespace Characters.StatusEffectSystems.StatusEffects
{
    public class FlowStageEffect : BaseStatusEffect<FlowStageEffectDataSo>
    {
        public override void OnStart(BaseController owner)
        {
            owner.CombatSystem.AddCurrentDamage(effectData.DamageIncrease);
            owner.CombatSystem.AddCurrentDamageMultiplierPercent(effectData.DamagePercentIncrease);
            owner.MovementSystem.AddCurrentSpeedMultiplier(effectData.SpeedPercentIncrease);

            if (!effectData.ExpandCamera) return;
            if (owner is not PlayerController player) return;
            player.CameraController.PushOrtho(
                player.CameraController.defaultOrthoSize + effectData.AdditionalExpandSize, duration, this, 0.25f);
        }

        public override void OnUpdate(BaseController owner, float deltaTime)
        {
            
        }

        public override void OnExit(BaseController owner)
        {
            owner.CombatSystem.AddCurrentDamage(-effectData.DamageIncrease);
            owner.CombatSystem.AddCurrentDamageMultiplierPercent(-effectData.DamagePercentIncrease);
            owner.MovementSystem.AddCurrentSpeedMultiplier(-effectData.SpeedPercentIncrease);

            if (!effectData.ExpandCamera) return;
            if (owner is not PlayerController player) return;
            player.CameraController.CancelByOwner(this);
        }
    }
}
