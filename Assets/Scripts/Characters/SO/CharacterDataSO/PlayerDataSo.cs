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
        [FoldoutGroup("Combat")]
        [SerializeField] private float baseExpLevelUp;
        
        [FoldoutGroup("Combat")]
        [SerializeField] private float expMultiplierPerLevel;

        [FoldoutGroup("Camera Settings")] [SerializeField]
        private CameraShakeOption attackHitCameraShakeOption;
        
        [FoldoutGroup("Camera Settings")] [SerializeField]
        private CameraShakeOption counterAttackHitCameraShakeOption;

        [FoldoutGroup("Camera Settings")] [SerializeField]
        private CameraOrthoOption counterAttackHitOrthoOption;
        
        [FoldoutGroup("Camera Settings")] [SerializeField]
        private CameraShakeOption takeDamageCameraShakeOption;
        
        public float BaseExpLevelUp => baseExpLevelUp;
        public float ExpMultiplierPerLevel => expMultiplierPerLevel;

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

        public List<BaseSkillDataSo> SkillUpgradePool => skillUpgradePool;
        public int UpgradeChoicesCount => upgradeChoicesCount;

        // Proxy for Odin (required non-inherited methods)
        public bool IsSkillUpgradePoolUniqueProxy(List<BaseSkillDataSo> pool) => IsSkillPoolUnique(pool);
        public bool IsAllSkillLv1Proxy(List<BaseSkillDataSo> pool) => IsAllLv1(pool);

        public CameraShakeOption AttackHitCameraShakeOption => attackHitCameraShakeOption;
        public CameraShakeOption CounterAttackHitCameraShakeOption => counterAttackHitCameraShakeOption;
        public CameraOrthoOption CounterAttackHitOrthoOption => counterAttackHitOrthoOption;

        public CameraShakeOption TakeDamageCameraShakeOption => takeDamageCameraShakeOption;
    }
}