using Characters.FeedbackSystems;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    
    [CreateAssetMenu(fileName = "SkillPiercerDashData", menuName = "GameData/SkillData/SkillPiercerDashData")]
    public class SkillPiercerDashDataSo : BaseSkillDataSo
    {
        [Unit(Units.Second)]
        [FoldoutGroup("Damage Configs")]
        [SerializeField] private float dashBaseDamage = 0;
        
        [Unit(Units.Percent)]
        [FoldoutGroup("Damage Configs")]
        [SerializeField] private float damageMultiplier = 100;
        
        [FoldoutGroup("Damage Configs")]
        [SerializeField] private float damageEnableDuration = 0.35f;
        
        [FoldoutGroup("Dash Configs")]
        [SerializeField] private float dashChargeTime = 2f;
        
        [FoldoutGroup("Dash Configs")]
        [PropertyTooltip("After charge state. disable sight input and wait for duration before dash.")]
        [SerializeField] private float dashPrepareDuration = 0.75f;
        
        [FoldoutGroup("Dash Configs")]
        [PropertyTooltip("Duration of the dash movement in seconds.")]
        [SerializeField] private float dashDuration = 0.3f;
        
        [FoldoutGroup("Dash Configs")]
        [PropertyTooltip("Total distance the character will dash forward.")]
        [SerializeField] private float dashDistance = 20f;
        
        [FoldoutGroup("Dash Configs")]
        [PropertyTooltip("AnimationCurve controlling the dash's easing over time. Used to shape the dash speed progression (e.g., accelerate then decelerate).")]
        [SerializeField] private AnimationCurve dashEaseCurve;
        
        [FoldoutGroup("Dash Configs")]
        [PropertyTooltip("AnimationCurve that applies lateral displacement during the dash allowing for custom arcing or wave-like motion paths instead of straight-line dashing.")]
        [SerializeField] private AnimationCurve dashMoveCurve;

        [FoldoutGroup("Feedback")] 
        [ValueDropdown("@FeedbackName.Odin.ShortGroupWithNone(\"Skill\")")] 
        [SerializeField] private string dashFeedback;
        

        public float DashBaseDamage => dashBaseDamage;

        public float DamageMultiplier => damageMultiplier;

        public float DamageEnableDuration => damageEnableDuration;

        public float DashChargeTime => dashChargeTime;

        public float DashPrepareDuration => dashPrepareDuration;

        public float DashDuration => dashDuration;

        public float DashDistance => dashDistance;

        public AnimationCurve DashEaseCurve => dashEaseCurve;

        public AnimationCurve DashMoveCurve => dashMoveCurve;

        public string DashFeedback => 
            string.IsNullOrEmpty(dashFeedback) ? null : FeedbackName.ResolveFullKey("Skill", dashFeedback);
    }
}
