using System.Collections.Generic;
using System.Linq;
using Characters.SO.SkillDataSo;
using Characters.SO.CharacterDataSO;
using UnityEngine;

namespace Characters.SkillSystems
{
    public class SkillUpgradeController : MonoBehaviour
    {
        [Header("Number of skill choices presented on level up.")]
        public int upgradeChoicesCount = 3;

        private SkillSystem _playerSkillSystem;
        private List<BaseSkillDataSo> _skillPool = new();

        public void AssignData(SkillSystem skillSystem, PlayerDataSo playerDataSo)
        {
            _playerSkillSystem = skillSystem;
            _skillPool = playerDataSo.SkillUpgradePool;
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
            var currentRoots = slotData.All.Select(s => s.RootNode).ToHashSet();

            // 1. Base node ยังไม่ถูกถือ (add new)
            options.AddRange(_skillPool.Where(s => !currentRoots.Contains(s)));

            // 2. อัพเกรดได้ (next)
            foreach (var skill in slotData.All)
            {
                if (skill != null && skill.NextSkillDataUpgrade != null)
                    options.Add(skill.NextSkillDataUpgrade);
            }

            options = options
                .Where(skill => skill != null)
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
