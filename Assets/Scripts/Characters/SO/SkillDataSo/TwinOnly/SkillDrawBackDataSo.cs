using System.Collections.Generic;
using Characters.FeedbackSystems;
using Characters.StatusEffectSystems;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Characters.SO.SkillDataSo.TwinOnly
{
    [CreateAssetMenu(fileName = "SkillDrawBackData", menuName = "GameData/SkillData/TwinOnly/SkillDrawBackData")]
    public class SkillDrawBackDataSo : BaseSkillDataSo
    {
        [FoldoutGroup("Damage Configs"), SerializeField]
        private float baseDamagePerHit = 5f;

        [Unit(Units.Percent)] [FoldoutGroup("Damage Configs"), SerializeField]
        private float damageMultiplier = 100;
        
        [FoldoutGroup("Damage Configs"), SerializeField]
        private float baseExplosionDamagePerHit = 5f;

        [Unit(Units.Percent)] [FoldoutGroup("Damage Configs"), SerializeField]
        private float explosionDamageMultiplier = 100;
        
        [FoldoutGroup("Damage Configs"), SerializeField]
        private float explosionRadius = 5f;
        
        [FoldoutGroup("Damage Configs"), SerializeField]
        private float knockBackDistance = 5f;
        
        [FoldoutGroup("Damage Configs"), SerializeField]
        private float knockBackDuration = 0.25f;

        [Unit(Units.Percent)]
        [FoldoutGroup("DrawBack Config"), SerializeField]
        private float availableOnHpLessOrEqualThan = 50f;

        [FoldoutGroup("DrawBack Config"), SerializeField]
        private float chargeTime = 3f;
        
        [FoldoutGroup("DrawBack Config"), SerializeField]
        private float followSpeed = 45f;
        
        [Tooltip("Stop duration before attack")]
        [FoldoutGroup("DrawBack Config"), SerializeField]
        private float fleeDuration = 0;
        
        [FoldoutGroup("DrawBack Config"), SerializeField]
        private float attackMergeBackDuration = 0.07f;
        
        [FoldoutGroup("Status Effects"), SerializeField]
        private List<StatusEffectDataPayload> effectSelfSplitOut;
        
        [FoldoutGroup("Status Effects"), SerializeField]
        private List<StatusEffectDataPayload> effectSelfOnSuccess;
        
        [FoldoutGroup("Feedback")] 
        [ValueDropdown("@FeedbackName.Odin.ShortGroupWithNone(\"Skill\")")] 
        [SerializeField] private string attackSuccessFeedback;
        
        public float BaseDamagePerHit => baseDamagePerHit;
        public float AvailableOnHpLessOrEqualThan => availableOnHpLessOrEqualThan;
        public float BaseExplosionDamagePerHit => baseExplosionDamagePerHit;
        public float ExplosionDamageMultiplier => explosionDamageMultiplier;
        public float ExplosionRadius => explosionRadius;
        public float KnockBackDistance => knockBackDistance;
        public float KnockBackDuration => knockBackDuration;
        public float DamageMultiplier => damageMultiplier;
        public float ChargeTime => chargeTime;
        public float FollowSpeed => followSpeed;
        public float FleeDuration => fleeDuration;
        public float AttackMergeBackDuration => attackMergeBackDuration;
        public List<StatusEffectDataPayload> EffectSelfSplitOut => effectSelfSplitOut;
        public List<StatusEffectDataPayload> EffectSelfOnSuccess => effectSelfOnSuccess;
        public string AttackSuccessFeedback => 
            string.IsNullOrEmpty(attackSuccessFeedback) ? null : FeedbackName.ResolveFullKey("Skill", attackSuccessFeedback);
    }
}
