using System;
using System.Collections.Generic;
using System.Linq;
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
        ChallengeAtLeast = 6,
        ParryAtLeast = 7,
        WinAtLeast = 8,
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

        [ShowIf(nameof(type), AchievementConditionType.ChallengeAtLeast)]
        public int challengeAtLeast;

        [ShowIf(nameof(type), AchievementConditionType.ParryAtLeast)]
        public int minParryAmountOnRun;

        [ShowIf(nameof(type), AchievementConditionType.WinAtLeast)]
        public int winAtLeast;
        
        // แสดงสรุปให้ดูอ่านง่าย (ไม่บังคับใช้ก็ได้)
        [ShowInInspector, ReadOnly]
        private string Summary =>
            type switch
            {
                AchievementConditionType.None                    => "(Always true)",
                AchievementConditionType.MapIdEquals             => $"Map = {mapId}",
                AchievementConditionType.ChallengeIdEquals       => $"Challenge = {challengeId}",
                AchievementConditionType.HighestScoreAtLeast     => $"HighestScore ≥ {minHighestScore}",
                AchievementConditionType.TotalDamageAtLeast      => $"TotalDamage ≥ {minTotalDamage}",
                AchievementConditionType.TotalKillAtLeast        => $"TotalKill {enemyId} ≥ {minKillAtLeast}",
                AchievementConditionType.ChallengeAtLeast        => $"Challenge >= {challengeAtLeast}",
                AchievementConditionType.ParryAtLeast            => $"Parry >= {minParryAmountOnRun}",
                AchievementConditionType.WinAtLeast              => $"Win >= {winAtLeast}",
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
                    current = Mathf.Min(current, target);
                    return true;

                case AchievementConditionType.TotalDamageAtLeast:
                    target  = minTotalDamage;
                    current = p.TotalDamageDeal;
                    current = Mathf.Min(current, target);
                    return true;
                
                case AchievementConditionType.TotalKillAtLeast:
                    target = minKillAtLeast;
                    p.TotalKill.TryGetValue(enemyId, out current);
                    current = Mathf.Min(current, target);
                    return true;
                
                case AchievementConditionType.ParryAtLeast:
                    target = minParryAmountOnRun;
                    current = Mathf.Min(p.HighestParryUseOnRun, target);
                    return true;
                
                case AchievementConditionType.WinAtLeast:
                    target = winAtLeast;
                    current = Mathf.Min(p.MapStats.Sum(w => w.Value.WinAmount), target);
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
        
        public bool TryGetMainProgress(PlayerData p, out int current, out int target)
        {
            current = 0;
            target  = 0;

            if (conditions == null || conditions.Count == 0 || p == null)
                return false;

            // รวบรวมทุก condition ที่มี progress
            var tmpCurrents = new List<int>();
            var tmpTargets  = new List<int>();

            foreach (var c in conditions)
            {
                if (c == null) continue;

                if (c.TryGetProgress(p, out var cCur, out var cTar))
                {
                    // กัน division by zero
                    if (cTar <= 0) continue;

                    tmpCurrents.Add(cCur);
                    tmpTargets.Add(cTar);
                }
            }

            if (tmpTargets.Count == 0)
                return false;

            if (logic == AchievementConditionLogic.And)
            {
                // AND: มองว่าต้องทำครบทุกเงื่อนไข
                // วิธีง่าย: รวมเป็นเป้ารวม แล้วใช้ sum(current)/sum(target)
                int sumCur = 0;
                int sumTar = 0;
                for (int i = 0; i < tmpTargets.Count; i++)
                {
                    sumCur += tmpCurrents[i];
                    sumTar += tmpTargets[i];
                }

                current = Mathf.Min(sumCur, sumTar); // กันไม่ให้เกินเป้า
                target  = sumTar;
                return true;
            }
            else // AchievementConditionLogic.Or
            {
                // OR: ผ่านอันไหนก็ได้ → ใช้ progress ที่ "ไกลสุด"
                float bestRatio = 0f;
                int bestCur = 0;
                int bestTar = 0;

                for (int i = 0; i < tmpTargets.Count; i++)
                {
                    float ratio = (float)tmpCurrents[i] / tmpTargets[i];
                    if (ratio > bestRatio)
                    {
                        bestRatio = ratio;
                        bestCur   = tmpCurrents[i];
                        bestTar   = tmpTargets[i];
                    }
                }

                if (bestTar <= 0)
                    return false;

                current = Mathf.Min(bestCur, bestTar);
                target  = bestTar;
                return true;
            }
        }
    }

    // ───────── Database SO ─────────

    [CreateAssetMenu(menuName = "Config/Achievement Database", fileName = "AchievementDatabase")]
    public class AchievementDatabase : SerializedScriptableObject
    {
        public List<AchievementEntry> entries = new();
    }
}
