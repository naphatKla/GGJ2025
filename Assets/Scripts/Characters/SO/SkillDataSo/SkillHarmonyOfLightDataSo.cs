using Characters.SkillSystems.SkillObjects;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    [CreateAssetMenu(fileName = "SkillHarmonyOfLightData", menuName = "GameData/SkillData/SkillHarmonyOfLightData")]
    public class SkillHarmonyOfLightDataSo : BaseSkillDataSo
    {
        [SerializeField] private int lightAmount = 4;
        [SerializeField] private HarmonyOfLightSkillObject lightPrefab;
        [SerializeField] private float spinDuration = 3f;
        [SerializeField] private float spinRatePerSec = 0.35f;
        [SerializeField] private float damageHitPerSec = 5f;

        public int LightAmount => lightAmount;
        public float SpinDuration => spinDuration;

        public HarmonyOfLightSkillObject LightPrefab => lightPrefab;

        public float SpinRatePerSec => spinRatePerSec;

        public float DamageHitPerSec => damageHitPerSec;
    }
}