using System.Collections.Generic;
using Characters.FeedbackSystems;
using Characters.StatusEffectSystems;
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

        [FoldoutGroup("Damage Configs")]
        [Tooltip("สถานะที่ติดให้ทุกตัวที่โดนระเบิด (เช่น Slow) - ว่าง = ทำดาเมจอย่างเดียว")]
        [SerializeField] private List<StatusEffectDataPayload> statusEffectOnHit = new();

        [FoldoutGroup("Skill Configs")] 
        [SerializeField] private bool stopOnCharge = true;
        
        [FoldoutGroup("Skill Configs")] 
        [SerializeField] private float chargeDuration;
        
        [FoldoutGroup("Skill Configs")]
        [SerializeField] private float explosionRadius;

        [FoldoutGroup("Feedback")]
        [ValueDropdown("@FeedbackName.Odin.ShortGroupWithNone(\"Skill\")")]
        [SerializeField] private string bombFeedback;

        public float BaseDamage => baseDamage;
        public float DamageMultiplier => damageMultiplier;
        public List<StatusEffectDataPayload> StatusEffectOnHit => statusEffectOnHit;

        public bool StopOnCharge => stopOnCharge;
        public float ChargeDuration => chargeDuration;
        public float ExplosionRadius => explosionRadius;

        public string BombFeedback => 
            string.IsNullOrEmpty(bombFeedback) ? null : FeedbackName.ResolveFullKey("Skill", bombFeedback);
    }
}
