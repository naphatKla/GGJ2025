using System.Collections.Generic;
using Characters.StatusEffectSystems;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

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
        private float lightStepSpeed = 40f;

        [FoldoutGroup("Light Step Configs"), SerializeField]
        private float minStepDistance = 8;

        [FoldoutGroup("Light Step Configs"), SerializeField]
        private int targetAmount = 9;
        
        [FoldoutGroup("Light Step Configs"), SerializeField]
        [PropertySpace(SpaceBefore = 5f, SpaceAfter = 10)]
        private List<AnimationCurve> randomCurve;
        
        [Unit(Units.Percent)] 
        [FoldoutGroup("Light Step Configs/Life Steal"), SerializeField]
        private float lifeStealPercentChance = 10;
        
        [Unit(Units.Percent)] 
        [FoldoutGroup("Light Step Configs/Life Steal"), SerializeField]
        private float lifeStealEffective = 10;
        
        [FoldoutGroup("Light Step Configs/Normal Phase"), SerializeField]
        private float normalPhaseSpeedStepUp = 0.5f;
        
        [FoldoutGroup("Light Step Configs/Normal Phase"), SerializeField]
        private float normalPhaseMaxSpeedMultiplier = 3f;
        
        [FormerlySerializedAs("godSpeedPhaseStartTime")] [FoldoutGroup("Light Step Configs/God Speed Phase"), SerializeField]
        private int godSpeedPhaseStartHit = 5;
        
        [FoldoutGroup("Light Step Configs/God Speed Phase"), SerializeField]
        private float godSpeedPhaseSpeedStepUp = 1f;
        
        [FoldoutGroup("Light Step Configs/God Speed Phase"), SerializeField]
        private float godSpeedPhaseMaxSpeedMultiplier = 6f;
        
        [FoldoutGroup("Status Effects"), SerializeField]
        private List<StatusEffectDataPayload> effectWhileLightStep;

        // Public accessors
        public float StartLightStepRadius => startLightStepRadius;
        public float LightStepRadius => lightStepRadius;
        public float TargetAmount => targetAmount;
        public float LightStepSpeed => lightStepSpeed;
        public float MinStepDistance => minStepDistance;
        public float LifeStealPercentChance => lifeStealPercentChance;
        public float LifeStealEffective => lifeStealEffective;
        public float NormalPhaseSpeedStepUp => normalPhaseSpeedStepUp;
        public float NormalPhaseMaxSpeedMultiplier => normalPhaseMaxSpeedMultiplier;
        public float GodSpeedPhaseStartHit => godSpeedPhaseStartHit;
        public float GodSpeedPhaseSpeedStepUp => godSpeedPhaseSpeedStepUp;
        public float GodSpeedPhaseMaxSpeedMultiplier => godSpeedPhaseMaxSpeedMultiplier;

        public List<StatusEffectDataPayload> EffectWhileLightStep => effectWhileLightStep;

        public List<AnimationCurve> RandomCurve => randomCurve;
    }
}
