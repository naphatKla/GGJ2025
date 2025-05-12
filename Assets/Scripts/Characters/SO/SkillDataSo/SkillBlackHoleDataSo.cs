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

        [SerializeField] private AnimationCurve explosionCurve;
        
        [Space] [Unit(Units.Second)]
        [SerializeField] private float skillDuration;
        
        [Space] [Unit(Units.Second)]
        [SerializeField] private float mergeInSpeed;

        [SerializeField] private AnimationCurve mergeInCurve;

        public int CloneAmount => cloneAmount;
        public float CloneDamage => cloneDamage;
        public float ExplosionDistance => explosionDistance;
        public float ExplosionSpeed => explosionSpeed;

        public AnimationCurve ExplosionCurve => explosionCurve;
        public float SkillDuration => skillDuration;
        public float MergeInSpeed => mergeInSpeed;

        public AnimationCurve MergeInCurve => mergeInCurve;
    }
}
