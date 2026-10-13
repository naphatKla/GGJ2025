using Characters.Controllers;
using Characters.SO.StatusEffectSO;

namespace Characters.StatusEffectSystems.StatusEffects
{
    /// <summary>
    /// Damage Resistance buff (Bright2's Perfect shape): adds its percentage to the owner's damage
    /// resistance pool for as long as it is active, and takes exactly that much back off when it ends.
    /// <see cref="HeathSystems.HealthSystem"/> reads the pool when applying HP damage.
    /// </summary>
    public class DamageResistanceEffect : BaseStatusEffect<DamageResistanceEffectDataSo>
    {
        /// <summary>Guards the add/remove pair so a re-apply or a double exit can't drift the pool.</summary>
        private bool _isApplied;

        public override void OnStart(BaseController owner)
        {
            if (_isApplied || owner == null || owner.StatusEffectSystem == null) return;

            _isApplied = true;
            owner.StatusEffectSystem.AddDamageResistancePercentage(effectData.ResistancePercentage);
        }

        public override void OnUpdate(BaseController owner, float deltaTime)
        {
        }

        public override void OnExit(BaseController owner)
        {
            if (!_isApplied || owner == null || owner.StatusEffectSystem == null) return;

            _isApplied = false;
            owner.StatusEffectSystem.AddDamageResistancePercentage(-effectData.ResistancePercentage);
        }
    }
}
