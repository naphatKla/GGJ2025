using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SkillSystems.SkillObjects
{
    /// <summary>
    /// Pooled visual/marker object for the Gravity Orb skill.
    /// All targeting, movement, pulling and explosion logic lives in
    /// <see cref="Characters.SkillSystems.SkillRuntimes.SkillGravityOrbRuntime"/> - this object just
    /// carries the visuals and knows how to reset itself for the pool.
    ///
    /// Also draws its own action ranges as gizmos (pull/explosion radius + pull-stop distance) so VFX
    /// can see exactly how big to scale their effects just by opening this prefab - no Play mode needed.
    /// The values shown are the "preview" ones below by default, and get overwritten to match the actual
    /// live skill data as soon as the runtime spawns this orb in-game (see <see cref="ConfigureRanges"/>).
    /// </summary>
    public class GravityOrbSkillObject : BaseSkillObject
    {
        [FoldoutGroup("VFX")]
        [SerializeField] private ParticleSystem pullFieldVfx;

        [FoldoutGroup("Gizmos")]
        [LabelText("Show Range Gizmos")]
        [SerializeField] private bool showRangeGizmos = true;

        [FoldoutGroup("Gizmos")]
        [LabelText("Preview: Effect Radius")]
        [PropertyTooltip("Preview only - mirrors SkillGravityOrbData.OrbEffectRadius. Radius used both for the continuous pull field and the end-of-life explosion. Overwritten at runtime by ConfigureRanges().")]
        [SerializeField] private float previewEffectRadius = 3.5f;

        [FoldoutGroup("Gizmos")]
        [LabelText("Preview: Pull Stop Distance")]
        [PropertyTooltip("Preview only - mirrors SkillGravityOrbData.PullStopDistance. Enemies stop being pulled closer once inside this distance from the orb's center.")]
        [SerializeField] private float previewPullStopDistance = 0.2f;

        [FoldoutGroup("Gizmos")]
        [SerializeField] private Color pullRangeColor = new(0.2f, 0.6f, 1f, 0.35f);

        [FoldoutGroup("Gizmos")]
        [SerializeField] private Color stopDistanceColor = new(1f, 0.25f, 0.25f, 0.5f);

        [FoldoutGroup("Gizmos")]
        [SerializeField] private Color explosionRangeColor = new(1f, 0.6f, 0.1f, 0.5f);

        // Set by SkillGravityOrbRuntime once the orb actually spawns, so gizmos track live skill data at runtime.
        private float? _liveEffectRadius;
        private float? _livePullStopDistance;
        private bool _isExploding;

        private float EffectRadius => _liveEffectRadius ?? previewEffectRadius;
        private float PullStopDistance => _livePullStopDistance ?? previewPullStopDistance;

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
        /// Pushes the actual configured ranges from the skill data into this instance so the gizmos drawn
        /// while the orb is alive in Play mode match reality instead of just the editor preview values.
        /// </summary>
        public void ConfigureRanges(float effectRadius, float pullStopDistance)
        {
            _liveEffectRadius = effectRadius;
            _livePullStopDistance = pullStopDistance;
        }

        /// <summary>
        /// Marks the explosion phase for gizmo coloring (draws the explosion ring instead of the pull ring).
        /// Purely cosmetic - has no effect on gameplay.
        /// </summary>
        public void MarkExploding(bool isExploding)
        {
            _isExploding = isExploding;
        }

        /// <summary>
        /// Full reset before returning to pool.
        /// </summary>
        public void ResetForPool()
        {
            SetPullFieldVisible(false);
            DamageOnTouch.DisableDamage(this);
            _liveEffectRadius = null;
            _livePullStopDistance = null;
            _isExploding = false;
        }

        private void OnDrawGizmos()
        {
            if (!showRangeGizmos) return;

            Vector3 center = transform.position;
            float effectRadius = EffectRadius;
            float stopDistance = PullStopDistance;

            // Outer ring: pull field while alive, explosion radius the instant it detonates (same value,
            // different color so VFX can tell the two phases apart at a glance).
            Color outer = _isExploding ? explosionRangeColor : pullRangeColor;
            Gizmos.color = outer;
            Gizmos.DrawWireSphere(center, effectRadius);
            Gizmos.color = new Color(outer.r, outer.g, outer.b, outer.a * 0.15f);
            Gizmos.DrawSphere(center, effectRadius);

            // Inner ring: the dead-zone where enemies stop being pulled closer.
            if (stopDistance > 0f)
            {
                Gizmos.color = stopDistanceColor;
                Gizmos.DrawWireSphere(center, stopDistance);
            }

#if UNITY_EDITOR
            UnityEditor.Handles.color = outer;
            UnityEditor.Handles.Label(center + Vector3.up * effectRadius,
                $"Gravity Orb\nEffect Radius: {effectRadius:0.##}\nStop Distance: {stopDistance:0.##}");
#endif
        }
    }
}
