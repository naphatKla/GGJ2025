using System.Collections.Generic;
using Characters.FeedbackSystems;
using Characters.StatusEffectSystems;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    [CreateAssetMenu(fileName = "SkillEnemyDashKnockbackData", menuName = "GameData/SkillData/SkillEnemyDashKnockbackData")]
    public class SkillEnemyDashKnockbackDataSo : BaseSkillDataSo
    {
        [FoldoutGroup("Damage Configs")]
        [SerializeField] private float dashBaseDamage = 0f;

        [Unit(Units.Percent)]
        [FoldoutGroup("Damage Configs")]
        [SerializeField] private float damageMultiplier = 100f;

        [FoldoutGroup("Damage Configs")]
        [SerializeField] private float damageEnableDuration = 0.25f;

        [FoldoutGroup("Dash Configs")]
        [SerializeField] private float dashChargeTime = 2f;

        [FoldoutGroup("Dash Configs")]
        [PropertyTooltip("After charge state, wait this duration before dash.")]
        [SerializeField] private float dashPrepareDuration = 0.75f;

        [FoldoutGroup("Dash Configs")]
        [SerializeField] private float dashDuration = 0.3f;

        [FoldoutGroup("Dash Configs")]
        [SerializeField] private float dashDistance = 8f;

        [FoldoutGroup("Dash Configs")]
        [SerializeField] private AnimationCurve dashEaseCurve;

        [FoldoutGroup("Dash Configs")]
        [SerializeField] private AnimationCurve dashMoveCurve;

        [FoldoutGroup("Knockback Configs")]
        [SerializeField] private float knockBackDistance = 5f;

        [FoldoutGroup("Knockback Configs")]
        [SerializeField] private float knockBackDuration = 0.25f;

        [FoldoutGroup("Knockback Configs")]
        [SerializeField] private bool knockBackOnlyOncePerSkill = true;

        [FoldoutGroup("Feedback")]
        [ValueDropdown("@FeedbackName.Odin.ShortGroupWithNone(\"Skill\")")]
        [SerializeField] private string dashFeedback;

        [FoldoutGroup("Status Effects", Order = 100)]
        [SerializeField] private List<StatusEffectDataPayload> effectsToTarget;

        public float DashBaseDamage => dashBaseDamage;
        public float DamageMultiplier => damageMultiplier;
        public float DamageEnableDuration => damageEnableDuration;
        public float DashChargeTime => dashChargeTime;
        public float DashPrepareDuration => dashPrepareDuration;
        public float DashDuration => dashDuration;
        public float DashDistance => dashDistance;
        public AnimationCurve DashEaseCurve => dashEaseCurve;
        public AnimationCurve DashMoveCurve => dashMoveCurve;
        public float KnockBackDistance => knockBackDistance;
        public float KnockBackDuration => knockBackDuration;
        public bool KnockBackOnlyOncePerSkill => knockBackOnlyOncePerSkill;
        public string DashFeedback => string.IsNullOrEmpty(dashFeedback) ? null : FeedbackName.ResolveFullKey("Skill", dashFeedback);
        public List<StatusEffectDataPayload> EffectsToTarget => effectsToTarget;
    }
}
