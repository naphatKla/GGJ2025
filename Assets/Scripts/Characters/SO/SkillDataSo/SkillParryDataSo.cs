using System;
using System.Collections.Generic;
using Characters.FeedbackSystems;
using Characters.StatusEffectSystems;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    [CreateAssetMenu(fileName = "SkillParryData", menuName = "GameData/SkillData/SkillParryData")]
    public class SkillParryDataSo : BaseSkillDataSo
    {
        [FoldoutGroup("Damage Configs")] [Title("Parry Succession")] [SerializeField]
        private float explosionBaseDamage = 0;

        [Unit(Units.Percent)] 
        [FoldoutGroup("Damage Configs")] [SerializeField]
        private float explosionDamageMultiplier = 100;
        
        [FoldoutGroup("Damage Configs")] 
        [SerializeField] private float explosionRadius;
        
        [FoldoutGroup("Parry Configs")] [SerializeField]
        private bool stopWhileParry;

        [FoldoutGroup("Parry Configs")] [SerializeField]
        private float parryColliderSizeMultiplier = 1;
        
        [FoldoutGroup("Parry Configs")] [SerializeField]
        private float parryDuration;
        
        [Tooltip("If parry success with in this % parry duration, determine that's a perfect parry")]
        [FoldoutGroup("Parry Configs")] [SerializeField] [Unit(Units.Percent)]
        private float perfectParryDurationPercentage;
        
        [FoldoutGroup("Parry Configs")] [SerializeField] [Unit(Units.Percent)]
        private float cooldownReduceOnPerfectParry;

        [FoldoutGroup("Parry Configs")] [SerializeField]
        private ParrySuccessData normalParrySuccess;

        [FoldoutGroup("Parry Configs")] [SerializeField]
        private ParrySuccessData perfectParrySuccess;

        [FoldoutGroup("Feedback")] 
        [ValueDropdown("@FeedbackName.Odin.ShortGroupWithNone(\"Skill\")")] 
        [SerializeField] private string parrySuccessFeedback;

        [FoldoutGroup("Status Effects")] [SerializeField]
        private List<StatusEffectDataPayload> selfEffectsOnParrySuccess;
        
        [FoldoutGroup("Status Effects")] [SerializeField]
        private List<StatusEffectDataPayload> explosionEffectsToTarget;
        
        public bool StopWhileParry => stopWhileParry;
        public float ParryColliderSizeMultiplier => parryColliderSizeMultiplier;
        public float ParryDuration => parryDuration;
        public float PerfectParryDurationPercentage => perfectParryDurationPercentage;
        public float ExplosionBaseDamage => explosionBaseDamage;
        public float ExplosionDamageMultiplier => explosionDamageMultiplier;
        public float ExplosionRadius => explosionRadius;
        public string ParrySuccessFeedback => 
            string.IsNullOrEmpty(parrySuccessFeedback) ? null : FeedbackName.ResolveFullKey("Skill", parrySuccessFeedback);
        public List<StatusEffectDataPayload> SelfEffectsOnParrySuccess => selfEffectsOnParrySuccess;
        public List<StatusEffectDataPayload> ExplosionEffectsToTarget => explosionEffectsToTarget;

        public ParrySuccessData NormalParrySuccess => normalParrySuccess;
        public ParrySuccessData PerfectParrySuccess => perfectParrySuccess;

        [Serializable]
        public struct ParrySuccessData
        {
            [SerializeField]
            public float knockBackDistance;

            [SerializeField]
            public float knockBackDuration;

            [SerializeField]
            public float healOnSuccess;
        }
    }
}