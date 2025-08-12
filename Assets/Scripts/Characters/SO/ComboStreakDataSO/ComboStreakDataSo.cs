using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.ComboStreakDataSO
{
    [CreateAssetMenu(menuName = "GameData/ComboStreak/ComboStreakData")]
    public class ComboStreakDataSo : ScriptableObject
    {
        [FoldoutGroup("Streak Configs"), Unit(Units.Second)]
        public float streakTimeoutSeconds = 4.5f;

        [FoldoutGroup("Streak Configs")]
        [MinValue(0)]
        public int maxRewardStreak = 50;

        [FoldoutGroup("Streak Configs"), Unit(Units.Percent)]
        [LabelText("On-Hit: Reduce Streak (%)")]
        [MinValue(0f)]
        public float onHitStreakReducePercent = 20f;     // เช่น 20 = ลด 20% ของสตรีคปัจจุบัน

        [FoldoutGroup("Boost Anergy"), Unit(Units.Percent)]
        [LabelText("Boost per Streak (%)")]
        [MinValue(0f)]
        public float boostPerStreakPercent = 10f;        // 10 = +10% ต่อ streak

        [FoldoutGroup("Boost Anergy"), Unit(Units.Percent)]
        [LabelText("Max Boost (%)")]
        [MinValue(0f)]
        public float maxBoostPercent = 500f;             // 500 = x5

        [FoldoutGroup("Boost Anergy"), Unit(Units.Percent)]
        [LabelText("On-Hit: Penalty of Max Boost (%)")]
        [MinValue(0f)]
        public float onHitBoostPenaltyPercentOfMax = 10f; // 10 = ลด 10% ของ Max Boost

        [FoldoutGroup("Flow Stage")]
        [MinValue(0)]
        public int stageIThreshold = 25;

        [FoldoutGroup("Flow Stage")]
        [MinValue(0)]
        public int stageIIThreshold = 50;

        [FoldoutGroup("Flow Stage"), Unit(Units.Second)]
        public float stageIIDuration = 20f;

        [FoldoutGroup("Flow Stage")]
        [MinValue(0)]
        public int stageIIEndReduceStreak = 25;          // จบแล้วตัด 25

        [FoldoutGroup("Flow Stage"), Unit(Units.Second)]
        public float stageIICooldownSeconds = 20f;

        // ----- Stage I -----
        [FoldoutGroup("Flow Stage/Flow Stage I Effect")]
        [LabelText("Stage I: +Flat Damage")]
        public int stageIDamageFlat = 17;

        // ----- Stage II -----
        [FoldoutGroup("Flow Stage/Flow Stage II Effect"), Unit(Units.Percent)]
        [LabelText("Stage II: +Damage (%)")]
        public float stageIIDamagePercent = 50f;         // 50 = +50%

        [FoldoutGroup("Flow Stage/Flow Stage II Effect"), Unit(Units.Percent)]
        [LabelText("Stage II: +Max Speed (%)")]
        public float stageIISpeedPercent = 50f;          // 50 = +50%

        [FoldoutGroup("Flow Stage/Flow Stage II Effect"), Unit(Units.Second)]
        [LabelText("Stage II: Dash Duration Δ (sec)")]
        public float stageIIDashDurationDeltaSeconds = -0.1f;

        [FoldoutGroup("Flow Stage/Flow Stage II Effect")]
        [LabelText("Stage II: Heal Every N Streaks")]
        [MinValue(1)]
        public int stageIIHealEveryNStreaks = 10;        // heal ทุก 10 streak ระหว่าง Stage II
    }
}
