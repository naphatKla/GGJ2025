using Characters.SkillSystems.SkillObjects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    [CreateAssetMenu(fileName = "SkillSpaceMineData", menuName = "GameData/SkillData/SkillSpaceMineData")]
    public class SkillSpaceMineDataSo : BaseSkillDataSo
    {
        [FoldoutGroup("Skill Object")]
        [SerializeField] [Required]
        private SpaceMineSkillObject spaceMineSkillObject;

        [FoldoutGroup("Skill Object")]
        [LabelText("Max Active Bombs")]
        [PropertyTooltip("Maximum bombs that can exist at once.")]
        [SerializeField] private int maxActiveBombs = 5;

        [FoldoutGroup("Timing")]
        [LabelText("Tag Duration (sec)")]
        [Unit(Units.Second)]
        [PropertyTooltip("How long the skill stays active, tagging enemies on hit.")]
        [SerializeField] private float tagDuration = 5f;

        [FoldoutGroup("Timing")]
        [LabelText("Bomb Countdown (sec)")]
        [Unit(Units.Second)]
        [PropertyTooltip("Time before the bomb detonates after being placed.")]
        [SerializeField] private float bombCountdown = 3f;

        [FoldoutGroup("Timing")]
        [LabelText("Damage Active Duration (sec)")]
        [Unit(Units.Second)]
        [PropertyTooltip("How long the damage hitbox stays active after detonation. Auto-cleans even if nothing is hit.")]
        [SerializeField] private float damageActiveDuration = 0.5f;

        [FoldoutGroup("Timing")]
        [LabelText("Cleanup Delay (sec)")]
        [Unit(Units.Second)]
        [PropertyTooltip("Delay before deactivating the bomb object after damage ends. Allows VFX to finish.")]
        [SerializeField] private float cleanupDelay = 0.35f;

        [FoldoutGroup("Damage")]
        [SerializeField] private float baseDamage = 20f;

        [FoldoutGroup("Damage")]
        [Unit(Units.Percent)]
        [SerializeField] private float damageMultiplier = 100f;

        public SpaceMineSkillObject SpaceMineSkillObject => spaceMineSkillObject;
        public int MaxActiveBombs => maxActiveBombs;
        public float TagDuration => tagDuration;
        public float BombCountdown => bombCountdown;
        public float DamageActiveDuration => damageActiveDuration;
        public float CleanupDelay => cleanupDelay;
        public float BaseDamage => baseDamage;
        public float DamageMultiplier => damageMultiplier;
    }
}