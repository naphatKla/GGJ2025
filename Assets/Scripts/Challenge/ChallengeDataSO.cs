using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Player;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Challenge
{
    public enum StatMode
    {
        Additive,
        Set
    }
    
    public enum PlayerAdditiveStat
    {
        MaxHP,
        ExpGain,
        BaseDamage,
        MoveSpeed,
    }
    
    public enum PlayerSetStat
    {
        MaxHP,
        BaseDamage,
        MoveSpeed
    }

    public enum EnemyStat
    {
        MaxHP,
        Damage,
        MoveSpeed,
        SpawnChance,
        RushMaxHP,
        RushDamage,
        RushMoveSpeed,
        RushSetChance,
    }

    [Serializable]
    public struct PlayerStatMod
    {
        public PlayerAdditiveStat stat;
        [Tooltip("ใส่ -10 = ลด 10%")] public float percentDelta;
    }
    
    [Serializable]
    public struct PlayerSetStatMod
    {
        public PlayerSetStat stat;
        [Tooltip("เช่น ใส่ HP 1 ก็จะ = HP 1 ในเกม")] public float setDelta;
    }
    
    [Serializable]
    public struct EnemyStatBundle
    {
        [Title("Normal Stats")]
        [Tooltip("+1% HP = +0.5%")]
        public float maxHP;
        [Tooltip("+1% DMG = +0.5%")]
        public float damage;
        [Tooltip("+1% MSPD = +1%")]
        public float moveSpeed;
        [Tooltip("+1% SPAWN CHANCE = +0.5%")]
        public float spawnChance;
        
        [Title("Rush Stats")][Space]
        [Tooltip("+1% HP = +0.5%")]
        public float rushMaxHP;
        [Tooltip("+1% DMG = +0.5%")]
        public float rushDamage;
        [Tooltip("+1% MSPD = +1%")]
        public float rushMoveSpeed;
        [Tooltip("+1% SPAWN CHANCE = +0.5%")]
        public float rushSetChance;
        public bool IsZero =>
            Mathf.Approximately(rushMaxHP, 0) &&
            Mathf.Approximately(rushDamage, 0) &&
            Mathf.Approximately(rushMoveSpeed, 0) &&
            Mathf.Approximately(rushSetChance, 0);
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
        [Tooltip("ID ศัตรูที่จะไม่รวมอยู่ในกลุ่มนี้หรือถูกนำออกจาก Global")]
        public List<string> enemyNotincluded;

        [FoldoutGroup("$groupName")]
        [Header("Stat Bundle (% from base)")]
        public EnemyStatBundle stats;
    }
    
    [Serializable]
    public struct AutoSelectedChallenge
    {
        public string autoSelectedMapID;
        public int selectOnLevel;
    }

    [CreateAssetMenu(menuName = "Challenge/ChallengeData", fileName = "ChallengeData")]
    public class ChallengeDataSO : ScriptableObject
    {
        public string id = "challenge_id";
        public string title = "Challenge Title";
        [TextArea(4, 10)] public string description = "Not assign description yet.";
        [TextArea(4, 10)] public string lockdescription = "Not assign description yet.";
        public bool hideFromUI;
        
        [Header("Map Filter")]
        [Tooltip("ถ้าว่าง = แสดงทุกแมพ, ถ้ามีค่า = แสดงเฉพาะแมพที่อยู่ในลิสต์นี้ เช่น map_voidmetro")]
        public List<string> allowedMapIds = new(); // "map_voidmetro"
        
        [Header("Auto Unlock")]
        [Tooltip("ถ้าเปิด จะปลดอัตโนมัติเมื่อเรียก AutoUnlock(...)")]
        public bool allowAutoUnlock = false;
        
        [Header("Auto Selected")]
        [Tooltip("ถ้าใส่แปลว่าแมพนั้นถูกเลือกแน่นอน เช่น map_voidmetro และตามด้วย Milestone")]
        public List<AutoSelectedChallenge> confirmSelectedMapIds = new(); // "map_voidmetro"

        public bool IsAvailableForMap(string mapId)
        {
            if (string.IsNullOrEmpty(mapId)) return true;
            if (allowedMapIds == null || allowedMapIds.Count == 0) return true;
            return allowedMapIds.Contains(mapId);
        }
        
        public bool IsConfirmSelectedForMap(string mapId, int milestoneLevel)
        {
            if (string.IsNullOrEmpty(mapId)) return false;
            if (confirmSelectedMapIds == null || confirmSelectedMapIds.Count == 0) return false;
            return confirmSelectedMapIds.Any(x => x.autoSelectedMapID == mapId && x.selectOnLevel == milestoneLevel);
        }
        
        public bool AutoUnlock(PlayerData player)
        {
            if (!allowAutoUnlock) return false;
            if (player == null) return false;
            if (string.IsNullOrEmpty(id)) return false;
            player.UnlockedChallenges ??= new HashSet<string>();
            if (player.UnlockedChallenges.Contains(id)) return false;
            
            player.UnlockedChallenges.Add(id);
            return true;
        }

        [FoldoutGroup("Score Modify")]
        [Header("Score Bonus (Flat)")] [Tooltip("เช่น ให้ +100% ก็ใส่ 100")]
        public float flatScoreBonusPercent;
        [FoldoutGroup("Score Modify")]
        public StatMode statMode;
        
        [FoldoutGroup("Player Modify")]
        [ShowIf("@statMode == StatMode.Set")] [Header("Player Set Stats")]
        public List<PlayerSetStatMod> playerSetMods = new();

        [FoldoutGroup("Player Modify")]
        [ShowIf("@statMode == StatMode.Additive")] [Header("Player Additive Debuffs (negative is harder)")]
        public List<PlayerStatMod> playerMods = new();
        
        [FoldoutGroup("Enemy Modify")]
        public bool disableAutoCalculate = false;
        [FoldoutGroup("Enemy Modify")]
        public List<EnemyGroupMod> enemyGroups = new();
        
        [FoldoutGroup("Stage Modify")] [Tooltip("เช่น 10 ก็จะบวกเวลาเพิ่มไป 10 วิ ถ้าใส่ -10 ก็จะลดลง 10 วิ")]
        public float timeModify;
        
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