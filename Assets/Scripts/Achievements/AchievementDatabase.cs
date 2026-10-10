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
        TakeHitLessThan = 9,
        HealAtLeast = 10,
        DiedAtLeast = 11,
        /// <summary>Won the given map at least N times (from the save's MapStats, so earlier wins count too).</summary>
        MapWinAtLeast = 12,
        /// <summary>Cleared milestone N (1 = first) or higher on the given map (MapStat.HighestMilestoneCleared).</summary>
        MapMilestoneClearedAtLeast = 13,
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

        [ShowIf("@type == AchievementConditionType.MapIdEquals || type == AchievementConditionType.MapWinAtLeast || type == AchievementConditionType.MapMilestoneClearedAtLeast")]
        [Tooltip("MapIdEquals: แมพที่ต้องเล่นจบ\nMapWinAtLeast / MapMilestoneClearedAtLeast: แมพที่ต้องชนะ / ผ่าน milestone เช่น map_voidmetro")]
        public string mapId;

        [ShowIf(nameof(type), AchievementConditionType.MapMilestoneClearedAtLeast)]
        [InfoBox("ผ่าน milestone Lv นี้ (หรือสูงกว่า) ของแมพ Map Id อย่างน้อย 1 ครั้ง\n"
                 + "Lv นับแบบเดียวกับหน้าเมนู (LV.0 – LV.4) เช่น 4 = LV.4 (Milestone_lv4)\n"
                 + "\"ผ่าน\" = ตามกติกาของ milestone ในแมพ (Win Required + เงื่อนไข score ใน Level Milestone ของ MapDataSO)\n"
                 + "นับเฉพาะรอบที่เล่นหลังเพิ่มระบบนี้ (save เก่าไม่มีประวัติ)")]
        [MinValue(0)]
        [LabelText("Milestone Lv")]
        [Tooltip("milestone Lv แบบเดียวกับหน้าเมนู: 0 = LV.0 (แรก), 4 = LV.4")]
        public int milestoneAtLeast;

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

        [ShowIf("@type == AchievementConditionType.WinAtLeast || type == AchievementConditionType.MapWinAtLeast")]
        [Tooltip("WinAtLeast: ชนะรวมทุกแมพกี่ครั้ง\nMapWinAtLeast: ชนะแมพ Map Id กี่ครั้ง (1 = ผ่านด่านนั้นครั้งแรก)")]
        public int winAtLeast;

        [ShowIf(nameof(type), AchievementConditionType.TakeHitLessThan)]
        public string takeHitFromId;

        [ShowIf(nameof(type), AchievementConditionType.TakeHitLessThan)]
        public int takeHitLessThan;

        [ShowIf(nameof(type), AchievementConditionType.HealAtLeast)]
        public int healAtLeastOnRun;

        [ShowIf(nameof(type), AchievementConditionType.DiedAtLeast)]
        public string diedFromId;

        [ShowIf(nameof(type), AchievementConditionType.DiedAtLeast)]
        public int diedAtLeast;

        // แสดงสรุปให้ดูอ่านง่าย (ไม่บังคับใช้ก็ได้)
        [ShowInInspector, ReadOnly]
        private string Summary =>
            type switch
            {
                AchievementConditionType.None => "(Always true)",
                AchievementConditionType.MapIdEquals => $"Map = {mapId}",
                AchievementConditionType.ChallengeIdEquals => $"Challenge = {challengeId}",
                AchievementConditionType.HighestScoreAtLeast => $"HighestScore ≥ {minHighestScore}",
                AchievementConditionType.TotalDamageAtLeast => $"TotalDamage ≥ {minTotalDamage}",
                AchievementConditionType.TotalKillAtLeast => $"TotalKill {enemyId} ≥ {minKillAtLeast}",
                AchievementConditionType.ChallengeAtLeast => $"Challenge >= {challengeAtLeast}",
                AchievementConditionType.ParryAtLeast => $"Parry >= {minParryAmountOnRun}",
                AchievementConditionType.WinAtLeast => $"Win >= {winAtLeast}",
                AchievementConditionType.TakeHitLessThan => $"TakeHit {takeHitFromId} < {takeHitLessThan}",
                AchievementConditionType.HealAtLeast => $"Heal >= {healAtLeastOnRun} On Run",
                AchievementConditionType.DiedAtLeast => $"Died from {diedFromId} >= {diedAtLeast}",
                AchievementConditionType.MapWinAtLeast => $"Win map {mapId} >= {winAtLeast}",
                AchievementConditionType.MapMilestoneClearedAtLeast => $"Clear {mapId} milestone Lv.{milestoneAtLeast} (or higher)",
                _ => ""
            };

        /// <summary>
        /// สำหรับ UI: คืน current / target ถ้าเงื่อนไขนี้เป็นแบบตัวเลข (มี progression ได้)
        /// เช่น HighestScoreAtLeast, TotalDamageAtLeast
        /// </summary>
        public bool TryGetProgress(PlayerData p, out int current, out int target)
        {
            current = 0;
            target = 0;
            if (p == null) return false;

            switch (type)
            {
                case AchievementConditionType.HighestScoreAtLeast:
                    target = minHighestScore;
                    current = p.HighestScore;
                    current = Mathf.Min(current, target);
                    return true;

                case AchievementConditionType.TotalDamageAtLeast:
                    target = minTotalDamage;
                    current = p.TotalDamageDeal;
                    current = Mathf.Min(current, target);
                    return true;

                case AchievementConditionType.TotalKillAtLeast:
                    target = minKillAtLeast;
                    p.totalKillDictionary.TryGetValue(enemyId, out current);
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

                case AchievementConditionType.DiedAtLeast:
                    target = diedAtLeast;

                    if (diedFromId == "*")
                        current = p.totalDiedDictionary.Sum(e => e.Value);
                    else
                        p.totalDiedDictionary.TryGetValue(diedFromId, out current);

                    current = Mathf.Min(current, target);
                    return true;

                case AchievementConditionType.MapWinAtLeast:
                    target = winAtLeast;
                    current = Mathf.Min(MapWins(p, mapId), target);
                    return true;

                case AchievementConditionType.MapMilestoneClearedAtLeast:
                    // shown as "levels cleared": clearing Lv.0..Lv.4 = 5 steps
                    target = Mathf.Max(0, milestoneAtLeast) + 1;
                    current = Mathf.Clamp(HighestMilestoneCleared(p, mapId) + 1, 0, target);
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>Times the player has won this map (0 when never played or unknown).</summary>
        public static int MapWins(PlayerData p, string mapId)
        {
            if (p?.MapStats == null || string.IsNullOrEmpty(mapId)) return 0;
            return p.MapStats.TryGetValue(mapId, out var stat) && stat != null ? stat.WinAmount : 0;
        }

        /// <summary>Highest milestone Lv cleared on this map (0 = LV.0), -1 = none.</summary>
        public static int HighestMilestoneCleared(PlayerData p, string mapId)
        {
            if (p?.MapStats == null || string.IsNullOrEmpty(mapId)) return -1;
            return p.MapStats.TryGetValue(mapId, out var stat) && stat != null ? stat.HighestMilestoneCleared : -1;
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
                AchievementRewardType.NanoCoin => $"+{nanoAmount} Nano",
                AchievementRewardType.UnlockMap => $"Unlock Map: {refId}",
                AchievementRewardType.UnlockChallenge => $"Unlock Challenge: {refId}",
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
        [FoldoutGroup("$displayName")] public string id;
        [FoldoutGroup("$displayName")] public string displayName;

        [FoldoutGroup("$displayName")] [TextArea]
        public string description;

        [FoldoutGroup("$displayName")] public Sprite icon;
        [FoldoutGroup("$displayName")] public AchievementTriggerType triggerType;

        [FoldoutGroup("$displayName")] public AchievementConditionLogic logic = AchievementConditionLogic.And;
        [FoldoutGroup("$displayName")] public List<AchievementConditionConfig> conditions = new();
        [FoldoutGroup("$displayName")] public List<AchievementRewardConfig> rewards = new();

        public bool TryGetMainProgress(PlayerData p, out int current, out int target)
        {
            current = 0;
            target = 0;

            if (p == null)
                return false;

            // ป้องกัน null
            conditions ??= new List<AchievementConditionConfig>();

            var tmpCurrents = new List<int>();
            var tmpTargets = new List<int>();

            // ดึง progress แบบตัวเลขจากแต่ละ condition (เฉพาะที่มี case ใน TryGetProgress)
            foreach (var c in conditions)
            {
                if (c == null) continue;

                if (c.TryGetProgress(p, out var cCur, out var cTar))
                {
                    if (cTar <= 0) continue; // กัน division by zero / เป้า 0

                    tmpCurrents.Add(cCur);
                    tmpTargets.Add(cTar);
                }
            }

            // ─────────────────────────────
            // 🔸 กรณี "ไม่มี numeric progress เลย" → default เป็น 0/1 หรือ 1/1 ตามปลดล็อก
            // ─────────────────────────────
            if (tmpTargets.Count == 0)
            {
                bool unlocked = p.UnlockedAchievements != null &&
                                p.UnlockedAchievements.Contains(id);

                current = unlocked ? 1 : 0; // ปลดแล้ว = 1/1, ยัง = 0/1
                target = 1;
                return true; // ให้ UI ใช้ค่า 0/1 นี้ได้เสมอ
            }

            // ─────────────────────────────
            // 🔹 กรณีมี numeric progress อย่างน้อย 1 อัน → ใช้ logic เดิม
            // ─────────────────────────────
            if (logic == AchievementConditionLogic.And)
            {
                // AND → มองรวมว่าเป็นเป้าใหญ่ก้อนเดียว
                int sumCur = 0;
                int sumTar = 0;
                for (int i = 0; i < tmpTargets.Count; i++)
                {
                    sumCur += tmpCurrents[i];
                    sumTar += tmpTargets[i];
                }

                current = Mathf.Min(sumCur, sumTar);
                target = sumTar;
                return true;
            }
            else // AchievementConditionLogic.Or
            {
                // OR → ใช้อันที่ progress เดินไกลสุด (ratio สูงสุด)
                float bestRatio = -1f;
                int bestCur = 0;
                int bestTar = 0;

                for (int i = 0; i < tmpTargets.Count; i++)
                {
                    float ratio = (float)tmpCurrents[i] / tmpTargets[i];
                    if (ratio > bestRatio)
                    {
                        bestRatio = ratio;
                        bestCur = tmpCurrents[i];
                        bestTar = tmpTargets[i];
                    }
                }

                if (bestTar <= 0)
                    return false;

                current = Mathf.Min(bestCur, bestTar);
                target = bestTar;
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