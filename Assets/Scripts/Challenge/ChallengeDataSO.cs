using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Challenge
{
    public enum PlayerStat
    {
        MaxHP,
        ExpGain,
        BaseDamage,
        MoveSpeed
    }

    public enum EnemyStat
    {
        MaxHP,
        Damage,
        MoveSpeed,
    }

    [Serializable]
    public struct PlayerStatMod
    {
        public PlayerStat stat;
        [Tooltip("ใส่ -10 = ลด 10%")] public float percentDelta;
    }
    
    [Serializable]
    public struct EnemyStatBundle
    {
        [Tooltip("+1% HP = +0.5%")]
        public float maxHP;
        [Tooltip("+1% DMG = +0.5%")]
        public float damage;
        [Tooltip("+1% MSPD = +1%")]
        public float moveSpeed;

        public bool IsZero =>
            Mathf.Approximately(maxHP, 0) &&
            Mathf.Approximately(damage, 0) &&
            Mathf.Approximately(moveSpeed, 0);
    }
    
    [Serializable]
    public class EnemyGroupMod
    {
        [FoldoutGroup("$groupName")]
        [Tooltip("ชื่อหมวดหมู่")]
        public string groupName;

        [FoldoutGroup("$groupName")]
        [Tooltip("ID ศัตรูที่อยู่ในกลุ่มนี้ (เช่น Enemy_Normal) | ใส่ \"*\" = ทุกตัว")]
        public List<string> enemyIds = new() { "Enemy_Normal" };

        [FoldoutGroup("$groupName")]
        [Header("Stat Bundle (% from base)")]
        public EnemyStatBundle stats;
    }

    [CreateAssetMenu(menuName = "Challenge/ChallengeData", fileName = "ChallengeData")]
    public class ChallengeDataSO : ScriptableObject
    {
        public string id = "challenge_id";
        public string title = "Challenge Title";
        [TextArea] public string description;

        [Header("Score Bonus (Flat)")] [Tooltip("เช่น ให้ +100% ก็ใส่ 100")]
        public float flatScoreBonusPercent;

        [Header("Player Debuffs (negative is harder)")]
        public List<PlayerStatMod> playerMods = new();

        [Header("Enemy Groups (edit per group once)")]
        public List<EnemyGroupMod> enemyGroups = new();
        
        // -------- Debug fields (show-only) --------
        [FoldoutGroup("Debug Score"), ReadOnly, ShowInInspector]
        private float TotalScore => _debug.totalPercent;

        [FoldoutGroup("Debug Score"), ReadOnly, ShowInInspector]
        private float Debug_PlayerScore => _debug.playerPercent;

        [FoldoutGroup("Debug Score"), ReadOnly, ShowInInspector]
        private float Debug_EnemiesScore => _debug.enemiesPercentSum;

        [FoldoutGroup("Debug Score"), Button(ButtonSizes.Medium)]
        private void RecalculateDebug() => RecomputeDebug();
        
        [NonSerialized] private ChallengeScoringUtility.ScoreBreakdown _debug;

        private void OnValidate()
        {
            RecomputeDebug();
        }

        private void RecomputeDebug()
        {
            _debug = ChallengeScoringUtility.Compute(this);
        }
    }
}