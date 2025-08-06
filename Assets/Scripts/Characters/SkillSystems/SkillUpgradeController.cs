using System;
using System.Collections.Generic;
using System.Linq;
using Characters.SO.SkillDataSo;
using Characters.SO.CharacterDataSO;
using UnityEngine;

namespace Characters.SkillSystems
{
    public class SkillUpgradeController : MonoBehaviour
    {
        private int _upgradeChoicesCount;
        private SkillSystem _playerSkillSystem;
        private List<BaseSkillDataSo> _skillPool = new();
        private List<BaseSkillDataSo> _currentOptions = new();
        public event Action<List<BaseSkillDataSo>> OnSkillUpgradeOptionsGenerated;

        public void AssignData(SkillSystem skillSystem, PlayerDataSo playerDataSo)
        {
            _playerSkillSystem = skillSystem;
            _skillPool = playerDataSo.SkillUpgradePool;
            _upgradeChoicesCount = playerDataSo.UpgradeChoicesCount;
        }

        public void OnLevelUp(int level)
        {
            _currentOptions = GetUpgradeOptions(_playerSkillSystem);

            if (_currentOptions.Count <= 0) return;

            foreach (var option in _currentOptions)
                Debug.Log($"[SkillUpgrade] Option: {option.name}, LV: {option.Level}");

            OnSkillUpgradeOptionsGenerated?.Invoke(_currentOptions);
            
            /*//TODO: Remove this if UI Implemented
            SelectSkill(_currentOptions[0]);*/
        }

        public void SelectSkill(BaseSkillDataSo selectedSkill)
        {
            if (selectedSkill == null || !_currentOptions.Contains(selectedSkill))
            {
                Debug.LogWarning($"[SkillUpgrade] Invalid skill selected: {selectedSkill?.name}");
                return;
            }

            UpgradeSkill(_playerSkillSystem, selectedSkill);
            Debug.Log($"[SkillUpgrade] Selected: {selectedSkill.name}, LV: {selectedSkill.Level}");

            _currentOptions.Clear(); // clear options after selection
        }

        private List<BaseSkillDataSo> GetUpgradeOptions(SkillSystem skillSystem)
        {
            var options = new List<BaseSkillDataSo>();
            if (skillSystem == null) return options;

            var slotData = skillSystem.GetAllCurrentSkillDatas();
            var currentRoots = slotData.All.Select(s => s.RootNode).ToHashSet();

            options.AddRange(_skillPool.Where(s => !currentRoots.Contains(s)));

            foreach (var skill in slotData.All)
            {
                if (skill != null && skill.NextSkillDataUpgrade != null)
                    options.Add(skill.NextSkillDataUpgrade);
            }

            options = options
                .Where(skill => skill != null)
                .Distinct()
                .ToList();

            int count = Mathf.Min(_upgradeChoicesCount, options.Count);
            var result = new List<BaseSkillDataSo>();
            var randomPool = new List<BaseSkillDataSo>(options);

            for (int i = 0; i < count; i++)
            {
                if (randomPool.Count == 0) break;
                int idx = UnityEngine.Random.Range(0, randomPool.Count);
                result.Add(randomPool[idx]);
                randomPool.RemoveAt(idx);
            }

            return result;
        }

        private void UpgradeSkill(SkillSystem skillSystem, BaseSkillDataSo chosenSkill)
        {
            skillSystem.UpgradeSkillSlot(chosenSkill);
        }

        public void ResetSkillUpgradeController()
        {
            _currentOptions.Clear();
        }
    }
}
