using System;
using System.Collections.Generic;
using Cameras;
using Challenge;
using Challenge.Challenge;
using Characters.SO.SkillDataSo;
using Characters.StatusEffectSystems;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Characters.SO.CharacterDataSO
{
    public enum PlayerDataStats
    {
        MaxHealth,
        Speed,
        Damage,
        CritRate,
        CritDamage,
        LifeStealChance,
        LifeStealEffective,
        ExpMultiply,
        PickupRadius,
        HurtIFrame,
        AutoSkillSlot,
    }
    
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

        [FoldoutGroup("Combat/Rank")]
        [ValidateInput(nameof(ValidateCombatRankDatas),
            "First rank must have rankPointThreshold = 0 and scoreMultiplier = 1")]
        [SerializeField]
        private List<CombatRankData> combatRankDatas = new();

        [FormerlySerializedAs("parryConditionData")] [FoldoutGroup("Combat/Rank")] [SerializeField]
        private CombatRankNormalParryConditionData normalParryConditionData;

        [FoldoutGroup("Combat/Rank")] [SerializeField]
        private CombatRankPerfectParryConditionData perfectParryConditionData;

        [FoldoutGroup("Combat/Rank")] [SerializeField]
        private CombatRankCounterDashConditionData counterDashConditionData;

        [FoldoutGroup("Combat/Rank")] [SerializeField]
        private CombatRankSingleKillConditionData singleKillConditionData;

        [FoldoutGroup("Combat/Rank")] [SerializeField]
        private CombatRankGroupKillConditionData groupKillConditionData;

        [FoldoutGroup("Combat/Rank")] [SerializeField]
        private CombatRankHealConditionData healConditionData;

        [FoldoutGroup("Combat/Rank")] [SerializeField]
        private CombatTakeDamageConditionData takeDamageConditionData;

        [FoldoutGroup("Combat/FlowState")] [SerializeField]
        private List<FlowStateData> flowStateDatas;
        
        [FoldoutGroup("Combat/FlowState")] [SerializeField] [Unit(Units.Second)]
        private int flowingMindLifeTimePerStack = 3;
        
        [FoldoutGroup("Combat/FlowState")] [SerializeField]
        private int flowingMindMaxCap = 10;

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
        public CombatRankNormalParryConditionData NormalParryConditionData => normalParryConditionData;
        public CombatRankPerfectParryConditionData PerfectParryConditionData => perfectParryConditionData;
        public CombatRankCounterDashConditionData CounterDashConditionData => counterDashConditionData;
        public CombatRankSingleKillConditionData SingleKillConditionData => singleKillConditionData;
        public CombatRankGroupKillConditionData GroupKillConditionData => groupKillConditionData;
        public CombatRankHealConditionData HealConditionData => healConditionData;
        public CombatTakeDamageConditionData TakeDamageConditionData => takeDamageConditionData;
        public List<FlowStateData> FlowStateDatas => flowStateDatas;
        public int FlowingMindLifeTimePerStack => flowingMindLifeTimePerStack;
        public int FlowingMindMaxCap => flowingMindMaxCap;
        

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
        
        public PlayerDataSo CopyInstance()
        {
            PlayerDataSo newDat = Instantiate(this);
            newDat.hideFlags = HideFlags.DontSave;
            return newDat;
        }

        public void AddPlayerStats(PlayerDataStats stats, float value)
        {
            switch (stats)
            {
                case PlayerDataStats.MaxHealth:
                    maxHealth = Mathf.CeilToInt(maxHealth + value);
                    break;
                case PlayerDataStats.Damage:
                    baseDamage = Mathf.CeilToInt(baseDamage + value);
                    break;
                case PlayerDataStats.Speed:
                    baseSpeed += value;
                    break;
                case PlayerDataStats.CritRate:
                    baseCriRate += value;
                    break;
                case PlayerDataStats.CritDamage:
                    baseCriDamage += value;
                    break;
                case PlayerDataStats.LifeStealChance:
                    baseLifeStealPercent += value;
                    break;
                case PlayerDataStats.LifeStealEffective:
                    baseLifeStealEffective += value;
                    break;
                case PlayerDataStats.PickupRadius:
                    pullItemRadius += value;
                    break;
                case PlayerDataStats.HurtIFrame:
                    invincibleTimePerHit += value;
                    break;
                case PlayerDataStats.AutoSkillSlot:
                    autoSkillSlot = Mathf.Max(0, Mathf.RoundToInt(autoSkillSlot + value));
                    break;
            }
        }
        
        public void MutiplyPlayerStats(PlayerDataStats stats, float value)
        {
            switch (stats)
            {
                case PlayerDataStats.MaxHealth:
                    maxHealth = Mathf.CeilToInt(maxHealth * value);
                    break;
                case PlayerDataStats.Damage:
                    baseDamage = Mathf.CeilToInt(baseDamage * value);
                    break;
                case PlayerDataStats.Speed:
                    baseSpeed *= value;
                    break;
                case PlayerDataStats.CritRate:
                    baseCriRate *= value;
                    break;
                case PlayerDataStats.CritDamage:
                    baseCriDamage *= value;
                    break;
                case PlayerDataStats.LifeStealChance:
                    baseLifeStealPercent *= value;
                    break;
                case PlayerDataStats.LifeStealEffective:
                    baseLifeStealEffective *= value;
                    break;
                case PlayerDataStats.HurtIFrame:
                    invincibleTimePerHit *= value;
                    break;
            }
        }
        
        public void SetPlayerStats(PlayerDataStats stats, float value)
        {
            switch (stats)
            {
                case PlayerDataStats.MaxHealth:
                    maxHealth = Mathf.CeilToInt(value);
                    break;
                case PlayerDataStats.Damage:
                    baseDamage = Mathf.CeilToInt(value);
                    break;
                case PlayerDataStats.Speed:
                    baseSpeed = value;
                    break;
            }
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

    [Serializable]
    public struct FlowStateData
    {
        public string flowStateId;

        [Tooltip("flowing mind = point threshold of each flow state")]
        public int flowingMindThreshold;

        public List<StatusEffectDataPayload> effectsApply;
    }

    public record CombatRankConditionData
    {
        public string conditionID;
        
        [PropertyOrder(999)]
        public int flowingMindModify;
    }
    
    [Serializable]
    public record CombatRankNormalParryConditionData : CombatRankConditionData
    {
        [Unit(Units.Percent)] [Tooltip("gain rank point when normal parry =  % amount of damage negate")]
        public float normalParryPercentage;
    }
    
    [Serializable]
    public record CombatRankPerfectParryConditionData : CombatRankConditionData
    {
        [Unit(Units.Percent)] [Tooltip("gain rank point when perfect parry =  % amount of damage negate")]
        public float perfectParryPercentage;
    }

    [Serializable]
    public record CombatRankCounterDashConditionData : CombatRankConditionData
    {
        [FormerlySerializedAs("normalCounterDashPercentage")] [Unit(Units.Percent)] [Tooltip("gain rank point when normal counter dash =  % amount of damage negate")]
        public float counterDashPercentage;
    }
    
    [Serializable]
    public record CombatRankSingleKillConditionData : CombatRankConditionData
    {
        public int pointAddedPerKill;
    }

    [Serializable]
    public record CombatRankGroupKillConditionData : CombatRankConditionData
    {
        public int killAmountToGainPoint;
        public float killWithInDuration;

        [Unit(Units.Percent)] [Tooltip("gain rank point when kill enemy within duration =  % amount of enemy score")]
        public float scorePercentage;
    }

    [Serializable]
    public record CombatRankHealConditionData : CombatRankConditionData
    {
        [Unit(Units.Percent)] [Tooltip("gain rank point when heal =  % of heal amount")]
        public float healPercentage;
    }

    [Serializable]
    public record CombatTakeDamageConditionData : CombatRankConditionData
    {
        [Unit(Units.Percent)] [Tooltip("lost rank point when take damage =  % of current rank point")]
        public float lostPointPercentage;
    }
}
