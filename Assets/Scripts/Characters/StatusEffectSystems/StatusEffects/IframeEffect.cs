using Characters.Controllers;
using Characters.HeathSystems;
using Characters.SO.StatusEffectSO;

namespace Characters.StatusEffectSystems.StatusEffects
{
    /// <summary>
    /// A status effect that makes the target invincible for the duration of the effect.
    /// Tied to the <see cref="HealthSystem"/> component to disable damage processing.
    /// </summary>
    public class IframeEffect : BaseStatusEffect<IframeEffectDataSo>
    {
        /// <summary>
        /// Called when the effect is applied to a GameObject.
        /// Enables invincibility via the HealthSystem. If not found, the effect is cleared immediately.
        /// </summary>
        /// <param name="owner">The GameObject receiving the effect.</param>
        public override void OnStart(BaseController owner)
        {
            owner.HealthSystem.SetInvincible(true);
        }

        /// <summary>
        /// This effect does not require per-frame updates, so the method is empty.
        /// </summary>
        /// <param name="deltaTime">Elapsed time since the last frame.</param>
        public override void OnUpdate(BaseController owner, float deltaTime) {}

        /// <summary>
        /// Called when the effect ends. Disables invincibility on the target.
        /// </summary>
        public override void OnExit(BaseController owner)
        {
            owner.HealthSystem.SetInvincible(false);
        }
 
    }
}