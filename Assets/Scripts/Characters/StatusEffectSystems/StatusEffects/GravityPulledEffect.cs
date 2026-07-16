using Characters.Controllers;
using Characters.SO.StatusEffectSO;
using Manager;

namespace Characters.StatusEffectSystems.StatusEffects
{
    /// <summary>
    /// Applied to a target while it's being pulled by a Gravity Orb.
    /// Locks out Primary and Secondary skill usage for as long as the effect is active
    /// (Auto skills are still allowed to trigger). Follows the same CC-immunity convention
    /// as <see cref="StunEffect"/>: IronBody blocks it outright, and invincible targets ignore it.
    /// </summary>
    public class GravityPulledEffect : BaseStatusEffect<GravityPulledEffectDataSo>
    {
        private bool _isApplied;

        public override void OnStart(BaseController owner)
        {
            if (owner.HealthSystem.IsInvincible)
            {
                ClearThisEffect();
                return;
            }

            if (StatusEffectManager.TryGetEffect(owner.gameObject, StatusEffectName.IronBody, out BaseStatusEffect _))
            {
                ClearThisEffect();
                return;
            }

            _isApplied = true;
            owner.SkillSystem.SetCanUsePrimary(false);
            owner.SkillSystem.SetCanUseSecondary(false);
        }

        public override void OnUpdate(BaseController owner, float deltaTime)
        {
        }

        public override void OnExit(BaseController owner)
        {
            if (!_isApplied) return;

            owner.SkillSystem.SetCanUsePrimary(true);
            owner.SkillSystem.SetCanUseSecondary(true);
        }
    }
}
