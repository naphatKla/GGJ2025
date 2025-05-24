using Characters.CombatSystems;
using Characters.SO.StatusEffectSO;
using UnityEngine;

namespace Characters.StatusEffectSystems.StatusEffects
{
    /// <summary>
    /// Status effect that temporarily enables the DamageOnTouch system for the affected character.
    /// When active, the character can deal contact damage to others on collision.
    /// Automatically disables the effect if no DamageOnTouch component is found.
    /// </summary>
    public class DamageOnTouchEffect : BaseStatusEffect<DamageOnTouchEffectDataSo>
    {
        /// <summary>
        /// Cached reference to the character's DamageOnTouch component,
        /// which controls collision-based damage output.
        /// </summary>
        private DamageOnTouch _damageOnTouch;
        
        /// <summary>
        /// Called when the status effect starts.
        /// Attempts to enable the DamageOnTouch component on the owning GameObject.
        /// If the component is missing, the effect self-clears.
        /// </summary>
        /// <param name="owner">The GameObject affected by the status effect.</param>
        public override void OnStart(GameObject owner)
        {
            if (!owner.TryGetComponent(out _damageOnTouch))
            {
                Debug.LogWarning("Owner damage on touch not found, remove buff damage on touch!!");
                ClearThisEffect();
                return;
            }
            
            _damageOnTouch.EnableDamage(true);
        }

        /// <summary>
        /// Called every frame while the effect is active.
        /// Currently unused for DamageOnTouchEffect, but reserved for future behavior (e.g., timed scaling).
        /// </summary>
        /// <param name="deltaTime">Time passed since the last frame.</param>
        public override void OnUpdate(float deltaTime)
        {
           
        }

        /// <summary>
        /// Called when the status effect ends.
        /// Disables the DamageOnTouch component to stop contact damage.
        /// </summary>
        public override void OnExit()
        {
            _damageOnTouch.EnableDamage(false);
        }
    }
}
