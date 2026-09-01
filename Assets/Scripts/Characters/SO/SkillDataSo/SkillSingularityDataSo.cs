using Characters.SO.StatusEffectSO;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    /// <summary>
    /// Data for Bright2's "Singularity" (Design Ver 0.0.20) - phase 3 only.
    /// <para/>
    /// "Stop every character around itself from action and movement (stun) for a set duration - basically
    /// a low cost time stop." It is not one long stun: the skill pulses at Trigger Per Second for its
    /// whole duration, and each pulse lays down a short stun. Anything that stays inside the radius is
    /// held continuously, while anything that escapes between pulses recovers on its own.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillSingularityData", menuName = "GameData/SkillData/TheBright2Only/Singularity")]
    public class SkillSingularityDataSo : BaseSkillDataSo
    {
        [FoldoutGroup("Singularity")]
        [LabelText("Skill Radius")]
        [PropertyTooltip("Spec: 150 - far wider than the Activate Radius, so anything that wandered close "
                         + "while the boss was deciding is caught too.")]
        [SerializeField] private float skillRadius = 150f;

        [FoldoutGroup("Singularity")]
        [LabelText("Duration")]
        [Unit(Units.Second), MinValue(0f)]
        [PropertyTooltip("Spec: 5s")]
        [SerializeField] private float duration = 5f;

        [FoldoutGroup("Singularity")]
        [LabelText("Trigger / Second")]
        [MinValue(0.01f)]
        [PropertyTooltip("How often the stun is re-applied while the skill runs. Spec: 2")]
        [SerializeField] private float triggerPerSecond = 2f;

        [FoldoutGroup("Singularity")]
        [LabelText("Stun Duration")]
        [Unit(Units.Second), MinValue(0f)]
        [PropertyTooltip("Length of the stun laid down by a single pulse. Spec: 0.6s - slightly longer than "
                         + "the gap between pulses, so staying inside means staying held.")]
        [SerializeField] private float stunDuration = 0.6f;

        [FoldoutGroup("Singularity")]
        [LabelText("Affected Layer")]
        [PropertyTooltip("Who gets caught. The doc says every character around the boss, so this normally "
                         + "covers the player AND other enemies. Narrow it to the enemy layer alone if the "
                         + "skill should not lock the player down.")]
        [SerializeField] private LayerMask affectedLayer;

        [FoldoutGroup("Singularity")]
        [LabelText("Stun Effect Data")]
        [Required]
        [PropertyTooltip("Stun effect applied by each pulse. Iron Body still blocks it, same as any stun.")]
        [SerializeField] private StunEffectDataSo stunEffectData;

        public float SkillRadius => skillRadius;
        public float Duration => duration;
        public float TriggerPerSecond => triggerPerSecond;
        public float StunDuration => stunDuration;
        public LayerMask AffectedLayer => affectedLayer;
        public StunEffectDataSo StunEffectData => stunEffectData;
    }
}
