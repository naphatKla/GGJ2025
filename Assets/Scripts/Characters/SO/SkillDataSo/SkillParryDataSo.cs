using System.Collections.Generic;
using Characters.StatusEffectSystems;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    [CreateAssetMenu(fileName = "SkillParryData", menuName = "GameData/SkillData/SkillParryData")]
    public class SkillParryDataSo : BaseSkillDataSo
    {
        [FoldoutGroup("Parry Configs")] [SerializeField]
        private bool stopWhileParry;

        [FoldoutGroup("Parry Configs")] [SerializeField]
        private float parryDuration;

        [FoldoutGroup("Parry Configs")] [Title("Parry Succession")] [SerializeField]
        private float explosionDamageMultiplier;

        [FoldoutGroup("Parry Configs")] 
        [SerializeField] private float explosionRadius;

        [FoldoutGroup("Parry Configs")] [SerializeField]
        private float knockBackDistance;

        [FoldoutGroup("Parry Configs")] [SerializeField]
        private float knockBackDuration;

        [FoldoutGroup("Status Effects")] [SerializeField]
        private List<StatusEffectDataPayload> explosionEffectsToTarget;

        public bool StopWhileParry => stopWhileParry;
        public float ParryDuration => parryDuration;
        public float ExplosionDamageMultiplier => explosionDamageMultiplier;
        public float ExplosionRadius => explosionRadius;
        public List<StatusEffectDataPayload> ExplosionEffectsToTarget => explosionEffectsToTarget;
        public float KnockBackDistance => knockBackDistance;
        public float KnockBackDuration => knockBackDuration;
    }
}