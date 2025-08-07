using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    [CreateAssetMenu(fileName = "SkillLightStepData", menuName = "GameData/SkillData/SkillLightStepData")]
    public class SkillLightStepDataSo : BaseSkillDataSo
    {
        [FoldoutGroup("Light Step Configs"), SerializeField]
        private float startLightStepRadius = 12f;

        [FoldoutGroup("Light Step Configs"), SerializeField]
        private float lightStepRadius = 8f;

        [FoldoutGroup("Light Step Configs"), SerializeField]
        private int targetAmount = 9;

        [Unit(Units.Percent)] 
        [FoldoutGroup("Light Step Configs"), SerializeField]
        private float lifeStealPercentChance = 10;
        
        [Unit(Units.Percent)] 
        [FoldoutGroup("Light Step Configs"), SerializeField]
        private float lifeStealEffective = 10;

        // Public accessors
        public float StartLightStepRadius => startLightStepRadius;
        public float LightStepRadius => lightStepRadius;
        public float TargetAmount => targetAmount;
        public float LifeStealPercentChance => lifeStealPercentChance;
        public float LifeStealEffective => lifeStealEffective;
    }
}
