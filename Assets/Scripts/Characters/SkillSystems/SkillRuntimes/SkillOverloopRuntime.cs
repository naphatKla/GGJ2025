using System.Collections.Generic;
using System.Threading;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Characters.SkillSystems.SkillRuntimes
{
    public class SkillOverloopRuntime : BaseSkillRuntime<SkillOverloopDataSo>, ISpecialConditionSkill
    {
        public bool IsWaitForCondition => owner.SkillSystem.CurrentAutoSkillActiveSlots <= 1;
        private readonly List<int> _buffedSlots = new();
        private float _factor = 1f;

        public override void PerformSkill()
        {
            if (IsWaitForCondition) return;
            if (IsCooldown || IsPerforming) return;
            
            base.PerformSkill();
        }

        protected override void OnSkillStart()
        {
            _buffedSlots.Clear();
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            int autoCount = owner.SkillSystem.CurrentAutoSkillActiveSlots;

            if (autoCount == 0) return;
            if (!skillData.CanTargetSelf && autoCount <= 1) return;

            int selfIndex = owner.SkillSystem.GetSlotIndex(skillData);

            if (selfIndex < 0)
            {
                Debug.LogError("Invalid index!");
                return;
            }

            // สร้าง candidate: auto slots = [2 .. 2+autoCount-1]
            var candidates = new List<int>(autoCount);
            for (int i = 0; i < autoCount; i++)
                candidates.Add(2 + i);

            if (!skillData.CanTargetSelf)
                candidates.Remove(selfIndex);

            if (candidates.Count == 0) return;

            int pickCount = Mathf.Clamp(skillData.TargetSkillAmount, 0, candidates.Count);

            // สุ่มไม่ซ้ำแบบสลับตำแหน่งหน้าแรก pickCount
            for (int i = 0; i < pickCount; i++)
            {
                int r = Random.Range(i, candidates.Count);
                (candidates[i], candidates[r]) = (candidates[r], candidates[i]);
                _buffedSlots.Add(candidates[i]);
            }

            if (_buffedSlots.Count == 0) return;

            _factor = 1f + (skillData.CooldownSpeedUpMultiplier / 100f);
            _factor = Mathf.Max(0f, _factor);

            foreach (var slot in _buffedSlots)
            {
                float cur = owner.SkillSystem.GetSlotCooldownMultiplier(slot);
                owner.SkillSystem.SetSlotCooldownMultiplier(slot, cur * _factor);
            }

            await UniTask.WaitForSeconds(skillData.OverloopDuration, cancellationToken: cancelToken);
        }

        protected override void OnSkillExit()
        {
            if (_buffedSlots.Count > 0 && _factor > 0f)
            {
                foreach (var slot in _buffedSlots)
                {
                    float cur = owner.SkillSystem.GetSlotCooldownMultiplier(slot);
                    owner.SkillSystem.SetSlotCooldownMultiplier(slot, cur / _factor);
                }
            }

            _buffedSlots.Clear();
            _factor = 1f;
        }
    }
}