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
        TotalDamageAtLeast = 4,
        TotalKillAtLeast = 5,
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

        [ShowIf(nameof(type), AchievementConditionType.TotalDamageAtLeast)]
        public int minTotalDamage;

        [ShowIf(nameof(type), AchievementConditionType.TotalKillAtLeast)]
        public string enemyId;
        [ShowIf(nameof(type), AchievementConditionType.TotalKillAtLeast)]
        public int minKillAtLeast;
        
        // แสดงสรุปให้ดูอ่านง่าย (ไม่บังคับใช้ก็ได้)
        [ShowInInspector, ReadOnly]
        private string Summary =>
            type switch
            {
                AchievementConditionType.None                    => "(Always true)",
                AchievementConditionType.MapIdEquals             => $"Map = {mapId}",
                AchievementConditionType.ChallengeIdEquals       => $"Challenge = {challengeId}",
                AchievementConditionType.HighestScoreAtLeast     => $"HighestScore ≥ {minHighestScore}",
                AchievementConditionType.TotalDamageAtLeast     => $"TotalDamage ≥ {minTotalDamage}",
                AchievementConditionType.TotalKillAtLeast     => $"TotalKill {enemyId} ≥ {minKillAtLeast}",
                _ => ""
            };
        
        /// <summary>
        /// สำหรับ UI: คืน current / target ถ้าเงื่อนไขนี้เป็นแบบตัวเลข (มี progression ได้)
        /// เช่น HighestScoreAtLeast, TotalDamageAtLeast
        /// </summary>
        public bool TryGetProgress(PlayerData p, out int current, out int target)
        {
            current = 0;
            target  = 0;
            if (p == null) return false;

            switch (type)
            {
                case AchievementConditionType.HighestScoreAtLeast:
                    target  = minHighestScore;
                    current = p.HighestScore;
                    return true;

                case AchievementConditionType.TotalDamageAtLeast:
                    target  = minTotalDamage;
                    current = p.TotalDamageDeal;
                    return true;

                default:
                    return false;
            }
        }
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
        [FoldoutGroup("$displayName")]
        public string id;
        [FoldoutGroup("$displayName")]
        public string displayName;
        [FoldoutGroup("$displayName")] [TextArea]
        public string description;
        [FoldoutGroup("$displayName")]
        public Sprite icon;
        [FoldoutGroup("$displayName")]
        public AchievementTriggerType triggerType;
        
        [FoldoutGroup("$displayName")]
        public AchievementConditionLogic logic = AchievementConditionLogic.And;
        [FoldoutGroup("$displayName")]
        public List<AchievementConditionConfig> conditions = new();
        [FoldoutGroup("$displayName")]
        public List<AchievementRewardConfig> rewards = new();
        
        /// <summary>
        /// สำหรับ UI: ขอ "progress หลัก" ของ achievement นี้
        /// ดีฟอลต์: ใช้เงื่อนไขแรกที่มี TryGetProgress ได้
        /// </summary>
        public bool TryGetMainProgress(PlayerData p, out int current, out int target)
        {
            current = 0;
            target  = 0;

            if (conditions == null || conditions.Count == 0) 
                return false;

            foreach (var c in conditions)
            {
                if (c != null && c.TryGetProgress(p, out current, out target))
                {
                    return true;
                }
            }

            return false;
        }
    }

    // ───────── Database SO ─────────

    [CreateAssetMenu(menuName = "Config/Achievement Database", fileName = "AchievementDatabase")]
    public class AchievementDatabase : SerializedScriptableObject
    {
        public List<AchievementEntry> entries = new();
    }
}
