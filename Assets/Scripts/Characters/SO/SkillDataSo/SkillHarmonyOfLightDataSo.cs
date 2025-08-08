using Characters.SkillSystems.SkillObjects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    [CreateAssetMenu(fileName = "SkillHarmonyOfLightData", menuName = "GameData/SkillData/SkillHarmonyOfLightData")]
    public class SkillHarmonyOfLightDataSo : BaseSkillDataSo
    {
        [FoldoutGroup("Skill Objects")] [SerializeField]
        private int lightAmount = 4;

        [FoldoutGroup("Skill Objects")] [SerializeField]
        private HarmonyOfLightSkillObject lightPrefab;

        [FoldoutGroup("Damage Configs")] [SerializeField]
        private float damageHitPerSec = 5f;
        
        [FoldoutGroup("Damage Configs")] [SerializeField]
        private float baseDamagePerHit = 40f;
        
        [Unit(Units.Percent)]
        [FoldoutGroup("Damage Configs")] [SerializeField]
        private float damageMultiplier = 100f;
        
        [FoldoutGroup("Harmony Configs")]
        [SerializeField] private float spinDuration = 3f;
        
        [FoldoutGroup("Harmony Configs")]
        [SerializeField] private float spinRatePerSec = 0.35f;

        public int LightAmount => lightAmount;
        public float SpinDuration => spinDuration;

        public HarmonyOfLightSkillObject LightPrefab => lightPrefab;

        public float SpinRatePerSec => spinRatePerSec;

        public float DamageHitPerSec => damageHitPerSec;
        
        public float BaseDamagePerHit => baseDamagePerHit;
        
        public float DamageMultiplier => damageMultiplier;
    }
}