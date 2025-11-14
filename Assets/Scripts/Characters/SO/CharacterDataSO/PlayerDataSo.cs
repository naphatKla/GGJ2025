using System;
using System.Collections.Generic;
using Cameras;
using Characters.SO.SkillDataSo;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.CharacterDataSO
{
    [CreateAssetMenu(fileName = "PlayerData", menuName = "GameData/CharacterData/PlayerData")]
    public class PlayerDataSo : BaseCharacterDataSo
    {
        [FoldoutGroup("Collect Item System")] [SerializeField]
        private float pullItemRadius = 8f;

        [FoldoutGroup("Combat")] [SerializeField]
        private float baseExpLevelUp;

        [FoldoutGroup("Combat")] [SerializeField]
        private int stepThreshold = 1;

        [FoldoutGroup("Combat")] [SerializeField]
        private float stepValue = 500;

        [FoldoutGroup("Combat")]
        [ValidateInput(nameof(ValidateCombatRankDatas),
            "First rank must have rankPointThreshold = 0 and scoreMultiplier = 1")]
        [SerializeField]
        private List<CombatRankData> combatRankDatas = new();

        [FoldoutGroup("Camera Settings")] [SerializeField]
        private CameraShakeOption attackHitCameraShakeOption;

        [FoldoutGroup("Camera Settings")] [SerializeField]
        private CameraShakeOption counterAttackHitCameraShakeOption;

        [FoldoutGroup("Camera Settings")] [SerializeField]
        private CameraShakeOption takeDamageCameraShakeOption;

        public float BaseExpLevelUp => baseExpLevelUp;
        public int StepThreshold => stepThreshold;
        public float StepValue => stepValue;
        public List<CombatRankData> CombatRankDatas => combatRankDatas;

        [Space]
        [FoldoutGroup("Skills/Upgrade")]
        [SerializeField, PropertyTooltip("The amount of choices to select the skill's upgrade.")]
        private int upgradeChoicesCount = 3;

        [FoldoutGroup("Skills/Upgrade")]
        [InfoBox("Only add root (Lv1) skills here. Used for random upgrade selection.")]
        [SerializeField,
         ValidateInput(nameof(IsSkillUpgradePoolUniqueProxy), "Duplicate skills are not allowed in the upgrade pool!"),
         ValidateInput(nameof(IsAllSkillLv1Proxy), "All skills in the upgrade pool must be Level 1!")]
        private List<BaseSkillDataSo> skillUpgradePool = new();

        public float PullItemRadius => pullItemRadius;
        public List<BaseSkillDataSo> SkillUpgradePool => skillUpgradePool;
        public int UpgradeChoicesCount => upgradeChoicesCount;
        public CameraShakeOption AttackHitCameraShakeOption => attackHitCameraShakeOption;
        public CameraShakeOption CounterAttackHitCameraShakeOption => counterAttackHitCameraShakeOption;
        public CameraShakeOption TakeDamageCameraShakeOption => takeDamageCameraShakeOption;

        // Proxy for Odin (required non-inherited methods)
        public bool IsSkillUpgradePoolUniqueProxy(List<BaseSkillDataSo> pool) => IsSkillPoolUnique(pool);
        public bool IsAllSkillLv1Proxy(List<BaseSkillDataSo> pool) => IsAllLv1(pool);

        private bool ValidateCombatRankDatas(List<CombatRankData> list)
        {
            if (list == null || list.Count == 0)
                return true;

            var first = list[0];

            return first.rankPointThreshold == 0 && Mathf.Approximately(first.scoreMultiplier, 1f);
        }
    }

    [Serializable]
    public struct CombatRankData
    {
        public string rankId;
        public int rankPointThreshold;

        [ValidateInput("@scoreMultiplier >= 1f", "Score Multiplier must to be >= 1")]
        public float scoreMultiplier;
    }
}