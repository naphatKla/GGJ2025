using System.Collections.Generic;
using System.Linq;
using Characters.SO.SkillDataSo;
using UnityEngine;

namespace Characters.SkillSystems
{
    public class SkillUpgradeController : MonoBehaviour
    {
        [Header("List of all base-level skills that can be acquired or upgraded.")]
        [SerializeField] private List<BaseSkillDataSo> allSkillDataList;

        [Header("Number of skill choices presented on level up.")]
        public int upgradeChoicesCount = 3;

        private SkillSystem playerSkillSystem;

        /// <summary>
        /// Injects the target SkillSystem for upgrades and management.
        /// </summary>
        public void Init(SkillSystem skillSystem)
        {
            playerSkillSystem = skillSystem;
        }

        /// <summary>
        /// Called when the player levels up. Picks upgrade options and applies upgrade (for demo, always picks the first).
        /// In production, you should present the options to UI and apply upgrade based on player selection.
        /// </summary>
        public void OnLevelUp(int level)
        {
            var options = GetUpgradeOptions(playerSkillSystem);

            // TODO: Send 'options' to UI or an event for the player to choose from.
            // For demo, always upgrade using the first available option.
            if (options.Count > 0)
            {
                UpgradeSkill(playerSkillSystem, options[0]);
            }
        }

        /// <summary>
        /// Returns a randomized list of upgradeable or new skills based on current slots and upgrade chains.
        /// </summary>
        public List<BaseSkillDataSo> GetUpgradeOptions(SkillSystem skillSystem)
        {
            var options = new List<BaseSkillDataSo>();
            if (skillSystem == null) return options;

            var slotData = skillSystem.GetAllCurrentSkillDatas();
            var currentOwned = slotData.All.ToHashSet();

            // Primary slot: add base skills if empty, or upgrade if possible
            if (slotData.Primary == null)
            {
                options.AddRange(allSkillDataList.Where(s => !currentOwned.Contains(s)));
            }
            else if (slotData.Primary.NextSkillDataUpgrade != null &&
                     !currentOwned.Contains(slotData.Primary.NextSkillDataUpgrade))
            {
                options.Add(slotData.Primary.NextSkillDataUpgrade);
            }

            // Secondary slot: add base skills if empty, or upgrade if possible
            if (slotData.Secondary == null)
            {
                options.AddRange(allSkillDataList.Where(s => !currentOwned.Contains(s)));
            }
            else if (slotData.Secondary.NextSkillDataUpgrade != null &&
                     !currentOwned.Contains(slotData.Secondary.NextSkillDataUpgrade))
            {
                options.Add(slotData.Secondary.NextSkillDataUpgrade);
            }

            // Auto skill slots: add base skills if empty, otherwise offer upgrades for each auto skill
            if (slotData.Auto.Count == 0)
            {
                options.AddRange(allSkillDataList.Where(s => !currentOwned.Contains(s)));
            }
            else
            {
                foreach (var autoSkill in slotData.Auto)
                {
                    if (autoSkill.NextSkillDataUpgrade != null &&
                        !currentOwned.Contains(autoSkill.NextSkillDataUpgrade))
                    {
                        options.Add(autoSkill.NextSkillDataUpgrade);
                    }
                }
            }

            // Filter duplicates and skills already owned
            options = options
                .Where(skill => skill != null && !currentOwned.Contains(skill))
                .Distinct()
                .ToList();

            // Randomly pick N skills (or less if there aren't enough)
            int count = Mathf.Min(upgradeChoicesCount, options.Count);
            var result = new List<BaseSkillDataSo>();
            var randomPool = new List<BaseSkillDataSo>(options);

            for (int i = 0; i < count; i++)
            {
                if (randomPool.Count == 0) break;
                int idx = Random.Range(0, randomPool.Count);
                result.Add(randomPool[idx]);
                randomPool.RemoveAt(idx);
            }

            return result;
        }

        /// <summary>
        /// Upgrades or adds a new skill to the player's SkillSystem (auto-detects appropriate slot).
        /// </summary>
        public void UpgradeSkill(SkillSystem skillSystem, BaseSkillDataSo chosenSkill)
        {
            skillSystem.UpgradeSkillSlot(chosenSkill);
        }

        public void ResetSkillUpgradeController()
        {
            
        }
    }
}
