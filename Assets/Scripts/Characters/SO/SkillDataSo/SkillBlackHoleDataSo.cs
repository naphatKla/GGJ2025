using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    [CreateAssetMenu(fileName = "SkillBlackHoleData", menuName = "GameData/SkillData/SkillBlackHoleData")]
    public class SkillBlackHoleDataSo : BaseSkillDataSo
    {
        [SerializeField] private int cloneAmount;
        [SerializeField] private float cloneDamage;
        
        [SerializeField] private float explosionDistance;
        
        [Unit(Units.Second)]
        [SerializeField] private float explosionSpeed;

        [SerializeField] private float explosionNoiseAmplitude;
        [SerializeField] private float explosionFrequency;

        [Space] [Unit(Units.Second)]
        [SerializeField] private float skillDuration;
        
        [Space] [Unit(Units.Second)]
        [SerializeField] private float mergeInSpeed;
        [SerializeField] private float mergeInNoiseAmplitude;
        [SerializeField] private float mergeInFrequency;

        public int CloneAmount => cloneAmount;
        public float CloneDamage => cloneDamage;
        public float ExplosionDistance => explosionDistance;
        public float ExplosionSpeed => explosionSpeed;
        public float ExplosionNoiseAmplitude => explosionNoiseAmplitude;
        public float ExplosionFrequency => explosionFrequency;
        public float SkillDuration => skillDuration;
        public float MergeInSpeed => mergeInSpeed;
        public float MergeInNoiseAmplitude => mergeInNoiseAmplitude;
        public float MergeInFrequency => mergeInFrequency;
    }
}
