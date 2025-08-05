using System.Collections.Generic;
using Characters.SO.SkillDataSo;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.CharacterDataSO
{
    [CreateAssetMenu(fileName = "PlayerData", menuName = "GameData/CharacterData/PlayerData")]
    public class PlayerDataSo : BaseCharacterDataSo
    {
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
    }
}