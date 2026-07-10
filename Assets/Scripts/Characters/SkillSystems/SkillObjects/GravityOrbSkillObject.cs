using UnityEngine;

namespace Characters.SkillSystems.SkillObjects
{
    /// <summary>
    /// Pooled visual/marker object for the Gravity Orb skill.
    /// All targeting, movement, pulling and explosion logic lives in
    /// <see cref="Characters.SkillSystems.SkillRuntimes.SkillGravityOrbRuntime"/> - this object just
    /// carries the visuals and knows how to reset itself for the pool.
    /// </summary>
    public class GravityOrbSkillObject : BaseSkillObject
    {
        [SerializeField] private ParticleSystem pullFieldVfx;

        /// <summary>
        /// Toggles the looping pull-field visual. Safe to call even if no VFX is assigned.
        /// </summary>
        public void SetPullFieldVisible(bool visible)
        {
            if (!pullFieldVfx) return;

            if (visible)
                pullFieldVfx.Play();
            else
                pullFieldVfx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        /// <summary>
        /// Full reset before returning to pool.
        /// </summary>
        public void ResetForPool()
        {
            SetPullFieldVisible(false);
            DamageOnTouch.DisableDamage(this);
        }
    }
}
