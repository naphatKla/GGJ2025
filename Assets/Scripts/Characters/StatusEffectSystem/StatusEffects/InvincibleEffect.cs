using Characters.HeathSystems;
using Characters.SO.StatusEffectSO;
using UnityEngine;

namespace Characters.StatusEffectSystem.StatusEffects
{
    /// <summary>
    /// A status effect that makes the target invincible for the duration of the effect.
    /// Tied to the <see cref="HealthSystem"/> component to disable damage processing.
    /// </summary>
    public class InvincibleEffect : BaseStatusEffect<InvincibleEffectDataSo>
    {
        #region Inspector & Variables
        
        /// <summary>
        /// Cached reference to the target's HealthSystem for toggling invincibility.
        /// </summary>
        private HealthSystem _ownerHealthSystem;
        
        #endregion
        
        #region Methods
        
        /// <summary>
        /// Called when the effect is applied to a GameObject.
        /// Enables invincibility via the HealthSystem. If not found, the effect is cleared immediately.
        /// </summary>
        /// <param name="owner">The GameObject receiving the effect.</param>
        public override void OnStart(GameObject owner)
        {
            if (!owner.TryGetComponent(out _ownerHealthSystem))
            {
                ClearThisEffect(); // Fail-safe: cancel effect if HealthSystem is missing
                return;
            }

            _ownerHealthSystem.SetInvincible(true);
        }

        /// <summary>
        /// This effect does not require per-frame updates, so the method is empty.
        /// </summary>
        /// <param name="deltaTime">Elapsed time since the last frame.</param>
        public override void OnUpdate(float deltaTime) {}

        /// <summary>
        /// Called when the effect ends. Disables invincibility on the target.
        /// </summary>
        public override void OnExit()
        {
            _ownerHealthSystem.SetInvincible(false);
        }
        
        #endregion
    }
}