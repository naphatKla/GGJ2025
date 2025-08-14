using System;
using System.Collections.Generic;
using Characters.SO.ComboStreakDataSO.StageDataSO;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.ComboStreakDataSO
{
    [Serializable]
    public class KillGrade
    {
        [MinValue(0)] public int minKillCount = 0;
        public string label = "D"; // D, C, B, A, S, SS, SSS
    }

    [Serializable]
    public class StageTierEntry
    {
        [MinValue(1)]
        public int minStreak = 25;

        [Required, AssetsOnly]
        public BaseComboStageSo stage;
    }

    [CreateAssetMenu(menuName = "GameData/ComboStreak/ComboStreakData/DefaultComboStreakData")]
    public class ComboStreakDataSo : ScriptableObject
    {
        [FoldoutGroup("Timer"), Unit(Units.Second)]
        public float comboTimeoutSeconds = 4.5f;

        [FoldoutGroup("Streak")]
        [MinValue(0)]
        public int maxRewardStreak = 50;

        [FoldoutGroup("Streak"), Unit(Units.Percent)]
        [LabelText("On-Hit: Reduce Streak (%)")]
        [MinValue(0)]
        public float onHitStreakReducePercent = 20f; // 20 = ลด 20%

        [FoldoutGroup("Boost"), Unit(Units.Percent)]
        [LabelText("Boost per Streak (%)")]
        [MinValue(0)]
        public float boostPerStreakPercent = 10f; // 10 = +10% ต่อสตรีค

        [FoldoutGroup("Boost"), Unit(Units.Percent)]
        [LabelText("Max Boost (%)")]
        [MinValue(0)]
        public float maxBoostPercent = 500f; // 500 = x5

        [FoldoutGroup("Boost"), Unit(Units.Percent)]
        [LabelText("On-Hit: Penalty of Max Boost (%)")]
        [MinValue(0)]
        public float onHitBoostPenaltyPercentOfMax = 10f; // 10 = -10% ของ Max

        // ===== Kill Grade =====
        [FoldoutGroup("Grades")]
        public List<KillGrade> killGrades = new()
        {
            new KillGrade{minKillCount=0,  label="D"},
            new KillGrade{minKillCount=10, label="C"},
            new KillGrade{minKillCount=25, label="B"},
            new KillGrade{minKillCount=50, label="A"},
            new KillGrade{minKillCount=75, label="S"},
            new KillGrade{minKillCount=100,label="SS"},
            new KillGrade{minKillCount=150,label="SSS"},
        };

        // ===== Stages as Tiers (minStreak -> Stage) =====
        [FoldoutGroup("Stages (Tiers)")]
        [InfoBox("เรียงจากเกณฑ์น้อย -> มาก; แต่ละ tier ทริกเกอร์ครั้งเดียวต่อรอบคอมโบ")]
        public List<StageTierEntry> stageTiers = new();

        // ===== Final Stage Controls (คุมโดย Manager) =====
        [FoldoutGroup("Final Stage Controls")]
        [Tooltip("รีเซ็ต Combo Timer เป็นค่าสูงสุดทันทีเมื่อเข้าสเตจสุดท้าย")]
        public bool finalStageResetComboTimerOnEnter = true;

        [FoldoutGroup("Final Stage Controls")]
        [Tooltip("ในสเตจสุดท้าย: แช่ Combo Timer ไม่ให้ลด")]
        public bool finalStageFreezeComboTime = true;

        [FoldoutGroup("Final Stage Controls")]
        [Tooltip("ในสเตจสุดท้าย: ป้องกันการลดสตรีคเมื่อโดนตี")]
        public bool finalStagePreventStreakDecrease = true;

        [FoldoutGroup("Final Stage Controls"), Unit(Units.Second)]
        [Tooltip("ถ้า > 0 จะจบสเตจสุดท้ายอัตโนมัติเมื่อเวลาหมด (คุมโดย Manager)")]
        public float finalStageAutoExitSeconds = 0f; // 0 = ไม่ออโต้

        [FoldoutGroup("Final Stage Controls")]
        [Tooltip("เมื่อจบสเตจสุดท้าย ให้ลดสตรีคลงจำนวนนี้")]
        [MinValue(0)]
        public int finalStageExitReduceStreak = 25;

        [FoldoutGroup("Final Stage Controls")]
        [Tooltip("เมื่อจบสเตจสุดท้าย รีเซ็ต progression ของสเตจ เพื่อให้ผู้เล่นวนเก็บใหม่")]
        public bool finalStageResetStageProgression = true;
    }
}
