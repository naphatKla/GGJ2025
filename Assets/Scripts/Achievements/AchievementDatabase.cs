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
        OnRunEnd = 0,
        OnRunWin = 1
    }

    public enum AchievementConditionType
    {
        None = 0,
        MapIdEquals = 1,
        ChallengeIdEquals = 2,
        HighestScoreAtLeast = 3,
    }

    public enum AchievementConditionLogic
    {
        And = 0,
        Or = 1,
    }

    public enum AchievementRewardType
    {
        NanoCoin = 0,
        UnlockMap = 1,
        UnlockChallenge = 2,
        UnlockPermanentUpgrade = 3,
    }

    /// <summary>Context ตอนเช็คเงื่อนไข</summary>
    public struct AchievementContext
    {
        public AchievementTriggerType TriggerType;

        public string MapId;
        public PlayerData Player;
    }

    // ───────── Condition / Reward config ─────────

    [Serializable]
    public class AchievementConditionConfig
    {
        public AchievementConditionType type;
        
        [ShowIf(nameof(type), AchievementConditionType.MapIdEquals)]
        public string mapId;
        
        [ShowIf(nameof(type), AchievementConditionType.ChallengeIdEquals)]
        public string challengeId;
        
        [ShowIf(nameof(type), AchievementConditionType.HighestScoreAtLeast)]
        public int minHighestScore;
        
        // แสดงสรุปให้ดูอ่านง่าย (ไม่บังคับใช้ก็ได้)
        [ShowInInspector, ReadOnly]
        private string Summary =>
            type switch
            {
                AchievementConditionType.None                    => "(Always true)",
                AchievementConditionType.MapIdEquals             => $"Map = {mapId}",
                AchievementConditionType.ChallengeIdEquals       => $"Challenge = {challengeId}",
                AchievementConditionType.HighestScoreAtLeast     => $"HighestScore ≥ {minHighestScore}",
                _ => ""
            };
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
