using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

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

        [SerializeField] private AnimationCurve explosionEaseCurve;
        [SerializeField] private AnimationCurve explosionMoveCurve;
        
        [Space] [Unit(Units.Second)]
        [SerializeField] private float mergeInSpeed;

        [SerializeField] private AnimationCurve mergeInEaseCurve;
        [SerializeField] private AnimationCurve mergeInMoveCurve;

        public int CloneAmount => cloneAmount;
        public float CloneDamage => cloneDamage;
        public float ExplosionDistance => explosionDistance;
        public float ExplosionSpeed => explosionSpeed;

        public AnimationCurve ExplosionEaseCurve => explosionEaseCurve;
        public AnimationCurve ExplosionMoveCurve => explosionMoveCurve;
        public float MergeInSpeed => mergeInSpeed;

        public AnimationCurve MergeInEaseCurve => mergeInEaseCurve;
        public AnimationCurve MergeInMoveCurve => mergeInMoveCurve;
    }
}
