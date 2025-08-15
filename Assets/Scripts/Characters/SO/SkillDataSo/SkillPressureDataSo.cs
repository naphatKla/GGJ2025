using Characters.FeedbackSystems;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    [CreateAssetMenu(fileName = "SkillPressureData", menuName = "GameData/SkillData/SkillPressureData")]
    public class SkillPressureDataSo : BaseSkillDataSo
    {
        [FoldoutGroup("Damage Configs")]
        [SerializeField] private float baseDamage;
        
        [Unit(Units.Percent)] [FoldoutGroup("Damage Configs")]
        [SerializeField] private float damageMultiplier = 100;
        
        [FoldoutGroup("Skill Configs")]
        [SerializeField] private float chargeDuration;
        
        [FoldoutGroup("Skill Configs")]
        [SerializeField] private float explosionRadius;

        [FoldoutGroup("Feedback")]
        [ValueDropdown("@FeedbackName.Odin.ShortGroupWithNone(\"Skill\")")]
        [SerializeField] private string bombFeedback;

        public float BaseDamage => baseDamage;
        public float DamageMultiplier => damageMultiplier;
        public float ChargeDuration => chargeDuration;
        public float ExplosionRadius => explosionRadius;

        public string BombFeedback => 
            string.IsNullOrEmpty(bombFeedback) ? null : FeedbackName.ResolveFullKey("Skill", bombFeedback);
    }
}
