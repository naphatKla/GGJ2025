using Characters.Controllers;
using Characters.SO.StatusEffectSO;

namespace Characters.StatusEffectSystems.StatusEffects
{
    public class FlowStageEffect : BaseStatusEffect<FlowStageEffectDataSo>
    {
        private float defaultOrthoSize;
        private float lastOrtho;
        
        public override void OnStart(BaseController owner)
        {
            owner.CombatSystem.AddCurrentDamage(effectData.DamageIncrease);
            owner.CombatSystem.AddCurrentDamageMultiplierPercent(effectData.DamagePercentIncrease);
            owner.MovementSystem.AddCurrentSpeedMultiplier(effectData.SpeedPercentIncrease);

            if (!effectData.ExpandCamera) return;
            if (owner is not PlayerController player) return;
            defaultOrthoSize = player.CameraController.defaultOrthoSize;
            lastOrtho = defaultOrthoSize + effectData.AdditionalExpandSize;
            
            player.CameraController.defaultOrthoSize = lastOrtho;
            if (player.CameraController.CurrentCam.m_Lens.OrthographicSize >= lastOrtho) return;
            player.CameraController.LerpOrthoSize(lastOrtho, 0.5f);
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
            player.CameraController.defaultOrthoSize = defaultOrthoSize;
            player.CameraController.ResetCamera();
        }
    }
}
