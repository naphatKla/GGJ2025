using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    /// <summary>
    /// Contains configurable parameters specific to the dash skill.
    /// Defines the distance, duration, motion curve, and optional noise used during the dash.
    /// Inherits common properties like cooldown and runtime binding from <see cref="BaseSkillDataSo"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillDashData", menuName = "GameData/SkillData/SkillDashData")]
    public class SkillDashDataSo : BaseSkillDataSo
    {
        [Unit(Units.Second)]
        [PropertyTooltip("Duration of the dash movement in seconds.")]
        [SerializeField] private float dashDuration = 0.3f;

        [PropertyTooltip("Total distance the character will dash forward.")]
        [SerializeField] private float dashDistance = 8f;

        [PropertyTooltip("Amplitude of the sinusoidal noise applied during dash (adds wavy offset). Set to 0 for straight dash.")]
        [SerializeField] private float dashNoiseAmplitude;

        [PropertyTooltip("Frequency of the sinusoidal noise pattern applied during dash.")]
        [SerializeField] private float dashNoiseFrequency;

        [PropertyTooltip("Easing function that controls how the dash speed interpolates over time.")]
        [SerializeField] private Ease easeType;

        [SerializeField] private AnimationCurve dashCurve;

        /// <summary>
        /// Duration of the dash movement in seconds.
        /// </summary>
        public float DashDuration => dashDuration;

        /// <summary>
        /// Total distance the character will dash forward.
        /// </summary>
        public float DashDistance => dashDistance;

        /// <summary>
        /// Amplitude of dash movement noise. Higher values produce more lateral deviation.
        /// </summary>
        public float DashNoiseAmplitude => dashNoiseAmplitude;

        /// <summary>
        /// Frequency of dash movement noise. Controls the number of oscillations during dash.
        /// </summary>
        public float DashNoiseFrequency => dashNoiseFrequency;

        /// <summary>
        /// Easing curve that defines the acceleration and deceleration behavior of the dash.
        /// </summary>
        public Ease EaseType => easeType;
        
        public AnimationCurve DashCurve => dashCurve;
    }
}
