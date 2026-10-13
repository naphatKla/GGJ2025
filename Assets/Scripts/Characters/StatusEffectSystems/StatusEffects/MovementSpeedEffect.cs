using Characters.Controllers;
using Characters.SO.StatusEffectSO;

namespace Characters.StatusEffectSystems.StatusEffects
{
    /// <summary>
    /// Applies a flat movement-speed change for as long as it is active and takes exactly the same amount
    /// back off when it ends. Used as the slow from Bright2's Piece of Mine fragments.
    /// </summary>
    public class MovementSpeedEffect : BaseStatusEffect<MovementSpeedEffectDataSo>
    {
        /// <summary>Guards the add/remove pair so a re-apply or a double exit can't drift the modifier.</summary>
        private bool _isApplied;

        public override void OnStart(BaseController owner)
        {
            if (_isApplied || owner == null || owner.MovementSystem == null) return;

            _isApplied = true;
            owner.MovementSystem.AddCurrentSpeedModifierPercentage(effectData.SpeedPercentChange);
        }

        public override void OnUpdate(BaseController owner, float deltaTime)
        {
        }

        public override void OnExit(BaseController owner)
        {
            if (!_isApplied || owner == null || owner.MovementSystem == null) return;

            _isApplied = false;
            owner.MovementSystem.AddCurrentSpeedModifierPercentage(-effectData.SpeedPercentChange);
        }
    }
}
