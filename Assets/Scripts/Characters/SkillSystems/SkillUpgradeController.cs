using System.Collections.Generic;
using System.Linq;
using Characters.SO.SkillDataSo;
using Characters.SO.CharacterDataSO; // ต้อง import ด้วย
using UnityEngine;

namespace Characters.SkillSystems
{
    public class SkillUpgradeController : MonoBehaviour
    {
        [Header("Number of skill choices presented on level up.")]
        public int upgradeChoicesCount = 3;

        private SkillSystem _playerSkillSystem;
        private List<BaseSkillDataSo> _skillPool = new();

        /// <summary>
        /// Inject PlayerDataSO (หรือ BaseCharacterDataSO ถ้าอยากรองรับหลายแบบ)
        /// </summary>
        public void AssignData(SkillSystem skillSystem, PlayerDataSo playerDataSo)
        {
            _playerSkillSystem = skillSystem;
            _skillPool = playerDataSo.SkillUpgradePool; // รับ pool จาก SO
        }

        public void OnLevelUp(int level)
        {
            var options = GetUpgradeOptions(_playerSkillSystem);

            if (options.Count <= 0) return;
            foreach (var baseSkillDataSo in options)
                Debug.Log($"Random Skill {baseSkillDataSo.name}, LV: {baseSkillDataSo.Level}");

            Debug.LogWarning($"Select skill {options[0].name}, LV: {options[0].Level}");
            UpgradeSkill(_playerSkillSystem, options[0]);
        }

        public List<BaseSkillDataSo> GetUpgradeOptions(SkillSystem skillSystem)
        {
            var options = new List<BaseSkillDataSo>();
            if (skillSystem == null) return options;

            var slotData = skillSystem.GetAllCurrentSkillDatas();
            var currentOwned = slotData.All.ToHashSet();

            // Primary
            if (slotData.Primary == null)
                options.AddRange(_skillPool.Where(s => !currentOwned.Contains(s)));
            else if (slotData.Primary.NextSkillDataUpgrade != null &&
                     !currentOwned.Contains(slotData.Primary.NextSkillDataUpgrade))
                options.Add(slotData.Primary.NextSkillDataUpgrade);

            // Secondary
            if (slotData.Secondary == null)
                options.AddRange(_skillPool.Where(s => !currentOwned.Contains(s)));
            else if (slotData.Secondary.NextSkillDataUpgrade != null &&
                     !currentOwned.Contains(slotData.Secondary.NextSkillDataUpgrade))
                options.Add(slotData.Secondary.NextSkillDataUpgrade);

            // Auto
            if (slotData.Auto.Count == 0)
                options.AddRange(_skillPool.Where(s => !currentOwned.Contains(s)));
            else
                foreach (var autoSkill in slotData.Auto)
                    if (autoSkill.NextSkillDataUpgrade != null &&
                        !currentOwned.Contains(autoSkill.NextSkillDataUpgrade))
                        options.Add(autoSkill.NextSkillDataUpgrade);

            // filter & random
            options = options
                .Where(skill => skill != null && !currentOwned.Contains(skill))
                .Distinct()
                .ToList();

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

        public void UpgradeSkill(SkillSystem skillSystem, BaseSkillDataSo chosenSkill)
        {
            skillSystem.UpgradeSkillSlot(chosenSkill);
        }

        public void ResetSkillUpgradeController() { }
    }
}
