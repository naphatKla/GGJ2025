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
    /// Also draws its own action ranges as gizmos (pull radius, explosion radius + pull-stop distance)
    /// so VFX can see exactly how big to scale their effects just by opening this prefab - no Play mode
    /// needed. The values shown are the "preview" ones below by default, and get overwritten to match the
    /// actual live skill data as soon as the runtime spawns this orb in-game (see
    /// <see cref="ConfigureRanges"/>).
    /// </summary>
    public class GravityOrbSkillObject : BaseSkillObject
    {
        [FoldoutGroup("VFX")]
        [SerializeField] private ParticleSystem pullFieldVfx;

        [FoldoutGroup("Gizmos")]
        [LabelText("Show Range Gizmos")]
        [SerializeField] private bool showRangeGizmos = true;

        [FoldoutGroup("Gizmos")]
        [LabelText("Preview: Pull Radius")]
        [PropertyTooltip("Preview only - mirrors SkillGravityOrbData.PullRadius. Radius that continuously pulls enemies in while the orb flies. Overwritten at runtime by ConfigureRanges().")]
        [SerializeField] private float previewPullRadius = 3.5f;

        [FoldoutGroup("Gizmos")]
        [LabelText("Preview: Explosion Radius")]
        [PropertyTooltip("Preview only - mirrors SkillGravityOrbData.ExplosionRadius. AOE radius when the orb's lifetime ends. Independent from Pull Radius. Overwritten at runtime by ConfigureRanges().")]
        [SerializeField] private float previewExplosionRadius = 3.5f;

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
        private float? _livePullRadius;
        private float? _liveExplosionRadius;
        private float? _livePullStopDistance;
        private bool _isExploding;

        private float PullRadius => _livePullRadius ?? previewPullRadius;
        private float ExplosionRadius => _liveExplosionRadius ?? previewExplosionRadius;
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
        public void ConfigureRanges(float pullRadius, float explosionRadius, float pullStopDistance)
        {
            _livePullRadius = pullRadius;
            _liveExplosionRadius = explosionRadius;
            _livePullStopDistance = pullStopDistance;
        }

        /// <summary>
        /// Marks the explosion phase for gizmo emphasis (tints the explosion ring instead of the pull
        /// ring). Purely cosmetic - has no effect on gameplay.
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
            _livePullRadius = null;
            _liveExplosionRadius = null;
            _livePullStopDistance = null;
            _isExploding = false;
        }

        private void OnDrawGizmos()
        {
            if (!showRangeGizmos) return;

            Vector3 center = transform.position;
            float pullRadius = PullRadius;
            float explosionRadius = ExplosionRadius;
            float stopDistance = PullStopDistance;

            // Pull radius - the field that drags enemies in while the orb is alive/flying.
            Gizmos.color = pullRangeColor;
            Gizmos.DrawWireSphere(center, pullRadius);

            // Explosion radius - the AOE when its lifetime ends. Drawn separately since it can differ
            // in size from the pull radius.
            Gizmos.color = explosionRangeColor;
            Gizmos.DrawWireSphere(center, explosionRadius);

            // Soft fill on whichever ring is the "active" one right now (cosmetic phase cue).
            Color active = _isExploding ? explosionRangeColor : pullRangeColor;
            float activeRadius = _isExploding ? explosionRadius : pullRadius;
            Gizmos.color = new Color(active.r, active.g, active.b, active.a * 0.15f);
            Gizmos.DrawSphere(center, activeRadius);

            // Inner ring: the dead-zone where enemies stop being pulled closer.
            if (stopDistance > 0f)
            {
                Gizmos.color = stopDistanceColor;
                Gizmos.DrawWireSphere(center, stopDistance);
            }

#if UNITY_EDITOR
            UnityEditor.Handles.color = active;
            Vector3 labelPos = center + Vector3.up * Mathf.Max(pullRadius, explosionRadius);
            UnityEditor.Handles.Label(labelPos,
                $"Gravity Orb\nPull Radius: {pullRadius:0.##}\nExplosion Radius: {explosionRadius:0.##}\nStop Distance: {stopDistance:0.##}");
#endif
        }
    }
}
