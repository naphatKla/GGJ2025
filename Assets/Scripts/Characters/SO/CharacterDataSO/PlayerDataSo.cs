using System;
using System.Collections.Generic;
using Cameras;
using Challenge;
using Challenge.Challenge;
using Characters.SO.SkillDataSo;
using Characters.StatusEffectSystems;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.CharacterDataSO
{
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

        [FoldoutGroup("Combat/Rank")] [SerializeField]
        private CombatRankParryConditionData parryConditionData;

        [FoldoutGroup("Combat/Rank")] [SerializeField]
        private CombatRankCounterDashConditionData counterDashConditionData;

        [FoldoutGroup("Combat/Rank")] [SerializeField]
        private CombatRankKillConditionData killConditionData;

        [FoldoutGroup("Combat/Rank")] [SerializeField]
        private CombatRankHealConditionData healConditionData;

        [FoldoutGroup("Combat/Rank")] [SerializeField]
        private CombatTakeDamageConditionData takeDamageConditionData;

        [FoldoutGroup("Combat/FlowState")] [SerializeField]
        private List<FlowStateData> flowStateDatas;

        [FoldoutGroup("Combat/FlowState")]
        [Tooltip("if the method increase rank point less than this value, will not gain a flowing mind stack")]
        [SerializeField] private int rankPointAddedThreshold = 5;
        
        [FoldoutGroup("Combat/FlowState")]
        [Tooltip("flowing mind = stack to enter each flow state, gain from every method increase rank point")]
        [SerializeField] private int flowingMindGainAmount = 1;
        
        [FoldoutGroup("Combat/FlowState")] [SerializeField]
        private int flowingMindReduceOnTakeDamage = 1;
        
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
        public CombatRankParryConditionData ParryConditionData => parryConditionData;
        public CombatRankCounterDashConditionData CounterDashConditionData => counterDashConditionData;
        public CombatRankKillConditionData KillConditionData => killConditionData;
        public CombatRankHealConditionData HealConditionData => healConditionData;
        public CombatTakeDamageConditionData TakeDamageConditionData => takeDamageConditionData;

        public List<FlowStateData> FlowStateDatas => flowStateDatas;
        
        public int RankPointAddedThreshold => rankPointAddedThreshold;
        public int FlowingMindGainAmount => flowingMindGainAmount;
        public int FlowingMindReduceOnTakeDamage => flowingMindReduceOnTakeDamage;
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
        
        public PlayerDataSo CopyInstance(PlayerSnapshot snap)
        {
            PlayerDataSo newDat = Instantiate(this);
            newDat.hideFlags = HideFlags.DontSave;
            // 1) SET
            if (snap.TryGetSet(PlayerSetStat.MaxHP, out var setHp))        newDat.maxHealth = setHp;
            if (snap.TryGetSet(PlayerSetStat.BaseDamage, out var setDmg))  newDat.baseDamage = setDmg;
            if (snap.TryGetSet(PlayerSetStat.MoveSpeed, out var setMspd))  newDat.baseSpeed = setMspd;

            // 2) Additive(%)
            newDat.maxHealth  *= 1f + snap.GetAdd(PlayerAdditiveStat.MaxHP) / 100f;
            newDat.baseDamage *= 1f + snap.GetAdd(PlayerAdditiveStat.BaseDamage) / 100f;
            newDat.baseSpeed  *= 1f + snap.GetAdd(PlayerAdditiveStat.MoveSpeed) / 100f;

            return newDat;
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

    [Serializable]
    public record CombatRankParryConditionData
    {
        [Unit(Units.Percent)] [Tooltip("gain rank point when perfect parry =  % amount of damage negate")]
        public float perfectParryPercentage;

        [Unit(Units.Percent)] [Tooltip("gain rank point when normal parry =  % amount of damage negate")]
        public float normalParryPercentage;
    }

    [Serializable]
    public record CombatRankCounterDashConditionData
    {
        [Unit(Units.Percent)] [Tooltip("gain rank point when perfect counter dash =  % amount of damage negate")]
        public float perfectCounterDashPercentage;

        [Unit(Units.Percent)] [Tooltip("gain rank point when normal counter dash =  % amount of damage negate")]
        public float normalCounterDashPercentage;
    }

    [Serializable]
    public record CombatRankKillConditionData
    {
        public int pointPerKill;
        [BoxGroup("Kill combo")]
        public int killAmountToGainPoint;
        [BoxGroup("Kill combo")]
        public float killWithInDuration;

        [Unit(Units.Percent)] [Tooltip("gain rank point when kill enemy within duration =  % amount of enemy score")]
        public float scorePercentage;
    }

    [Serializable]
    public record CombatRankHealConditionData
    {
        [Unit(Units.Percent)] [Tooltip("gain rank point when heal =  % of heal amount")]
        public float healPercentage;
    }

    [Serializable]
    public record CombatTakeDamageConditionData
    {
        [Unit(Units.Percent)] [Tooltip("lost rank point when take damage =  % of current rank point")]
        public float lostPointPercentage;
    }
}