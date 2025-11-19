using System;
using System.Collections.Generic;
using PermanentUpgrade;
using Player;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Achievements
{
    // ───────── Enums & Context ─────────

    public enum AchievementTriggerType
    {
        OnRunEnd,
        OnMapClear,
    }

    public enum AchievementConditionType
    {
        None,
        MapIdEquals,
        HighestScoreAtLeast,
    }

    public enum AchievementConditionLogic
    {
        And,
        Or
    }

    public enum AchievementRewardType
    {
        NanoCoin,
        UnlockMap,
        UnlockChallenge,
        UnlockPermanentUpgrade,
    }

    /// <summary>Context ตอนเช็คเงื่อนไข</summary>
    public struct AchievementContext
    {
        public AchievementTriggerType TriggerType;

        public string MapId;
        public int FinalScore;
        public float SurviveSeconds;
        public bool IsClear;

        public PlayerData Player;

        public int TotalKill;
        public int PermanentLevelChanged;
    }

    // ───────── Condition / Reward config ─────────

    [Serializable]
    public class AchievementConditionConfig
    {
        public AchievementConditionType type;

        // แสดงสรุปให้ดูอ่านง่าย (ไม่บังคับใช้ก็ได้)
        [ShowInInspector, ReadOnly]
        private string Summary =>
            type switch
            {
                AchievementConditionType.None                    => "(Always true)",
                AchievementConditionType.MapIdEquals             => $"Map = {mapId}",
                AchievementConditionType.HighestScoreAtLeast     => $"HighestScore ≥ {minHighestScore}",
                _ => ""
            };
        
        [ShowIf(nameof(type), AchievementConditionType.MapIdEquals)]
        public string mapId;
        
        [ShowIf(nameof(type), AchievementConditionType.HighestScoreAtLeast)]
        public int minHighestScore;
    }

    [Serializable]
    public class AchievementRewardConfig
    {
        public AchievementRewardType type;

        [ShowInInspector, ReadOnly]
        private string Summary =>
            type switch
            {
                AchievementRewardType.NanoCoin               => $"+{nanoAmount} Nano",
                AchievementRewardType.UnlockMap              => $"Unlock Map: {refId}",
                AchievementRewardType.UnlockChallenge        => $"Unlock Challenge: {refId}",
                AchievementRewardType.UnlockPermanentUpgrade => $"Unlock Perm: {permanentType}",
                _ => ""
            };

        [ShowIf(nameof(type), AchievementRewardType.NanoCoin)]
        public int nanoAmount;

        [ShowIf("@type == AchievementRewardType.UnlockMap || type == AchievementRewardType.UnlockChallenge")]
        public string refId;

        [ShowIf(nameof(type), AchievementRewardType.UnlockPermanentUpgrade)]
        public PermanentUpgradeType permanentType;
    }

    // ───────── Achievement Entry ─────────

    [Serializable]
    public class AchievementEntry
    {
        public string id;
        public string displayName;
        public string description;
        public Sprite icon;
        public AchievementTriggerType triggerType;
        
        public AchievementConditionLogic logic = AchievementConditionLogic.And;
        public List<AchievementConditionConfig> conditions = new();
        public List<AchievementRewardConfig> rewards = new();
    }

    // ───────── Database SO ─────────

    [CreateAssetMenu(menuName = "Config/Achievement Database", fileName = "AchievementDatabase")]
    public class AchievementDatabase : SerializedScriptableObject
    {
        public List<AchievementEntry> entries = new();
    }
}
