using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Characters.SO.SkillDataSo
{
    [CreateAssetMenu(fileName = "SkillOverloopData", menuName = "GameData/SkillData/SkillOverloopData")]
    public class SkillOverloopDataSo : BaseSkillDataSo
    {
        [FoldoutGroup("Overloop Configs")] [Min(0)]
        [SerializeField] private float overloopDuration;
        
        [FoldoutGroup("Overloop Configs")] [Min(0)]
        [SerializeField] private int targetSkillAmount;
        
        [FoldoutGroup("Overloop Configs")]
        [SerializeField] private bool canTargetSelf;
        
        [FoldoutGroup("Overloop Configs")]
        [Unit(Units.Percent)] [SerializeField]
        private float cooldownSpeedUpMultiplier = 0;
        
        public float OverloopDuration => overloopDuration;
        public bool CanTargetSelf => canTargetSelf;
        public int TargetSkillAmount => targetSkillAmount;
        public float CooldownSpeedUpMultiplier => cooldownSpeedUpMultiplier;
    }
}
