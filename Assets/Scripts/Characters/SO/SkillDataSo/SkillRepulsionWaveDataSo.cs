using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    [CreateAssetMenu(fileName = "SkillRepulsionWaveData", menuName = "GameData/SkillData/SkillRepulsionWaveData")]
    public class SkillRepulsionWaveDataSo : BaseSkillDataSo
    {
        [FoldoutGroup("Damage Configs")] [Title("Parry Succession")] [SerializeField]
        private float explosionBaseDamage = 0;

        [Unit(Units.Percent)] 
        [FoldoutGroup("Damage Configs")] [SerializeField]
        private float explosionDamageMultiplier = 100;
        
        [FoldoutGroup("Damage Configs")] 
        [SerializeField] private float explosionRadius;
        
        [FoldoutGroup("Repulsion Configs")]
        [SerializeField] private float knockBackDistance;

        [FoldoutGroup("Repulsion Configs")]
        [SerializeField] private float knockBackDuration;

        public float ExplosionBaseDamage => explosionBaseDamage;
        public float ExplosionRadius => explosionRadius;
        public float ExplosionDamageMultiplier => explosionDamageMultiplier;
        public float KnockBackDistance => knockBackDistance;
        public float KnockBackDuration => knockBackDuration;
    }
}
