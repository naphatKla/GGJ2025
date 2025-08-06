using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Characters.SO.SkillDataSo;
using Characters.SO.CharacterDataSO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Characters.SkillSystems
{
    public class SkillUpgradeController : MonoBehaviour
    {
        private int _upgradeChoicesCount;
        private SkillSystem _playerSkillSystem;
        private List<BaseSkillDataSo> _skillPool = new();
        private List<BaseSkillDataSo> _currentOptions = new();

        private int _pendingForSelection = 0;

        private CancellationTokenSource _globalCts;

        public event Action<List<BaseSkillDataSo>> OnSkillUpgradeOptionsGenerated;

        public void AssignData(SkillSystem skillSystem, PlayerDataSo playerDataSo)
        {
            _playerSkillSystem = skillSystem;
            _skillPool = playerDataSo.SkillUpgradePool;
            _upgradeChoicesCount = playerDataSo.UpgradeChoicesCount;
            _globalCts = new CancellationTokenSource();
        }

        public void OnLevelUp(int level)
        {
            UpgradeSkillAsync().Forget();
        }

        private async UniTaskVoid UpgradeSkillAsync()
        {
            // local cts + linked with global & mono destroy
            using var localCts = new CancellationTokenSource();
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                localCts.Token,
                _globalCts?.Token ?? CancellationToken.None,
                this.GetCancellationTokenOnDestroy()
            );

            try
            {
                await UniTask.WaitUntil(() => _pendingForSelection <= 0, cancellationToken: linkedCts.Token);

                _pendingForSelection++;
                _currentOptions = GetRandomAvailableUpgradeOptions(_playerSkillSystem);

                if (_currentOptions.Count <= 0)
                {
                    _pendingForSelection = 0;
                    return;
                }

                OnSkillUpgradeOptionsGenerated?.Invoke(_currentOptions);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[SkillUpgrade] Cancelled upgrade selection.");
            }
        }

        public void SelectSkill(BaseSkillDataSo selectedSkill)
        {
            if (_pendingForSelection <= 0)
            {
                Debug.LogWarning("No pending upgrade to apply selection to.");
                return;
            }

            _pendingForSelection--;

            if (selectedSkill == null || !_currentOptions.Contains(selectedSkill))
            {
                Debug.LogWarning($"[SkillUpgrade] Invalid skill selected: {selectedSkill?.name}");
                return;
            }

            UpgradeSkill(_playerSkillSystem, selectedSkill);
            Debug.Log($"[SkillUpgrade] Selected: {selectedSkill.name}, LV: {selectedSkill.Level}");

            _currentOptions.Clear();
        }

        private List<BaseSkillDataSo> GetRandomAvailableUpgradeOptions(SkillSystem skillSystem)
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
            _globalCts?.Cancel();
            _globalCts?.Dispose();
            _globalCts = new CancellationTokenSource();

            _currentOptions.Clear();
            _pendingForSelection = 0;

            Debug.Log("[SkillUpgrade] Reset and cancelled all pending upgrades.");
        }

        private void OnDestroy()
        {
            _globalCts?.Cancel();
            _globalCts?.Dispose();
            _globalCts = null;
        }
    }
}
