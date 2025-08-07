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

        [FoldoutGroup("Light Step Configs"), SerializeField]
        private float lightStepSpeed = 40f;

        [FoldoutGroup("Light Step Configs"), SerializeField]
        private float increaseSpeedAfterDurationPass = 3f;

        [FoldoutGroup("Light Step Configs"), SerializeField]
        private float increaseSpeedMultiplier = 3f;

        // Public accessors
        public float StartLightStepRadius => startLightStepRadius;
        public float LightStepRadius => lightStepRadius;
        public float TargetAmount => targetAmount;
        public float LifeStealPercentChance => lifeStealPercentChance;
        public float LifeStealEffective => lifeStealEffective;
        public float LightStepSpeed => lightStepSpeed;
        public float IncreaseSpeedAfterDurationPass => increaseSpeedAfterDurationPass;
        public float IncreaseSpeedMultiplier => increaseSpeedMultiplier;
    }
}
