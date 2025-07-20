using System.Collections.Generic;
using Characters.SO.SkillDataSo;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.CharacterDataSO
{
    public abstract class BaseCharacterDataSo : ScriptableObject
    {
        [Title("Health Data")]
        [SerializeField, MinValue(1), PropertyTooltip("Maximum health value.")]
        private float maxHealth = 100;

        [SerializeField, MinValue(0), PropertyTooltip("Invincible time after hit (sec).")]
        private float invincibleTimePerHit = 0.1f;

        [Title("Movement Data")]
        [SerializeField, PropertyTooltip("Base movement speed (unit/sec).")]
        private float baseSpeed = 5;

        [SerializeField, PropertyTooltip("Move acceleration rate.")]
        private float moveAccelerationRate = 15;

        [SerializeField, PropertyTooltip("Turn acceleration rate.")]
        private float turnAccelerationRate = 30;

        [Title("Combat Data")]
        [SerializeField, PropertyTooltip("Base damage per hit.")]
        private float baseDamage = 10;

        [Title("Skill Loadout")]
        [SerializeField, ValidateInput(nameof(IsSkillDataUnique), "Primary/Secondary/Auto skill must not duplicate!")]
        private BaseSkillDataSo primarySkillData;

        [SerializeField, ValidateInput(nameof(IsSkillDataUnique), "Primary/Secondary/Auto skill must not duplicate!")]
        private BaseSkillDataSo secondarySkillData;

        [SerializeField, MinValue(0), PropertyTooltip("Number of auto skill slots.")]
        private int autoSkillSlot = 3;

        [SerializeField, ValidateInput(nameof(IsSkillListUnique), "Duplicate skill in autoSkillDataList is not allowed!")]
        private List<BaseSkillDataSo> autoSkillDataList = new();

        // Validation
        private bool IsSkillListUnique(List<BaseSkillDataSo> list)
        {
            if (list == null) return true;
            var hashSet = new HashSet<BaseSkillDataSo>();
            foreach (var skill in list)
            {
                if (skill == null) continue;
                if (!hashSet.Add(skill)) return false;
            }
            return true;
        }
        private bool IsSkillDataUnique(BaseSkillDataSo _)
        {
            var set = new HashSet<BaseSkillDataSo>();
            if (primarySkillData && !set.Add(primarySkillData)) return false;
            if (secondarySkillData && !set.Add(secondarySkillData)) return false;
            foreach (var skill in autoSkillDataList)
            {
                if (skill == null) continue;
                if (!set.Add(skill)) return false;
            }
            return true;
        }

        // Public Getters
        public float MaxHealth => maxHealth;
        public float InvincibleTimePerHit => invincibleTimePerHit;
        public float BaseSpeed => baseSpeed;
        public float MoveAccelerationRate => moveAccelerationRate;
        public float TurnAccelerationRate => turnAccelerationRate;
        public float BaseDamage => baseDamage;

        public BaseSkillDataSo PrimarySkillData => primarySkillData;
        public BaseSkillDataSo SecondarySkillData => secondarySkillData;
        public int AutoSkillSlot => autoSkillSlot;
        public List<BaseSkillDataSo> AutoSkillDataList => autoSkillDataList;
    }
}
