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
            Mathf.Approximately(maxHP, 0) &&
            Mathf.Approximately(damage, 0) &&
            Mathf.Approximately(moveSpeed, 0) &&
            Mathf.Approximately(spawnChance, 0) &&
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

        [FoldoutGroup("Enemy Spawn (Milestone)")]
        [InfoBox("ตารางเกิดศัตรูเฉพาะ milestone นี้ (ใช้เมื่อไฟล์นี้อยู่ใน MilestoneContainer)\n"
                 + "• ปิด Override (ค่าเริ่มต้น) = ใช้ Enemy Spawn Mode ของแมพตามปกติ\n"
                 + "• เปิด Override + มีกฎ = กฎด้านล่างทำงาน แล้วเลือกได้ว่าจะแทนที่ / ต่อจาก / เล่นพร้อมโหมดของแมพ\n"
                 + "ช่วง Rush ระบบ Rush คุมเสมอ | Pattern และ MapEvent ทำงานปกติ")]
        [GUIColor("@this.overrideMapSpawn ? Color.green : Color.red")]
        [LabelText("Override Map Spawn"), LabelWidth(175)]
        [Tooltip("เปิด = ตอนเล่น milestone นี้ ใช้ Spawn Rules ด้านล่าง (ร่วมกับโหมดแมพตาม After Milestone)\n"
                 + "ปิด = ใช้ Enemy Spawn Mode ของแมพเหมือนเดิม")]
        public bool overrideMapSpawn;

        [FoldoutGroup("Enemy Spawn (Milestone)")]
        [ShowIf(nameof(overrideMapSpawn))]
        [InfoBox("Override เปิดอยู่แต่ยังไม่มีกฎที่ใช้ได้ milestone นี้จะใช้โหมดของแมพไปก่อน", InfoMessageType.Warning,
            "@this.overrideMapSpawn && !this.HasUsableSpawnRules")]
        [EnumToggleButtons, LabelText("After Milestone"), LabelWidth(175)]
        [Tooltip("Replace = ใช้กฎของ milestone อย่างเดียวทั้งเกม\n"
                 + "Then Map Mode = ใช้กฎของ milestone ก่อน แล้วส่งต่อให้โหมดของแมพ (ตั้งจุดส่งต่อที่ Hand Over)\n"
                 + "Together = ใช้กฎของ milestone และโหมดของแมพพร้อมกัน (เช่น เพิ่มบอสทับลงบนแมพปกติ)")]
        public GameControl.SO.MilestoneSpawnFlow spawnFlow = GameControl.SO.MilestoneSpawnFlow.Replace;

        [FoldoutGroup("Enemy Spawn (Milestone)")]
        [ShowIf("@this.overrideMapSpawn && this.spawnFlow == GameControl.SO.MilestoneSpawnFlow.ThenMapMode")]
        [EnumToggleButtons, LabelText("Hand Over"), LabelWidth(175)]
        [Tooltip("เมื่อไหร่จะเปลี่ยนจากกฎของ milestone ไปเป็นโหมดของแมพ\n"
                 + "At Time = ที่วินาทีที่กำหนด\n"
                 + "When Rules Finish = เมื่อกฎทุกข้อจบ (ถึง Expire At หรือครบ Max Total)")]
        public GameControl.SO.MilestoneHandOver spawnHandOver = GameControl.SO.MilestoneHandOver.AtTime;

        [FoldoutGroup("Enemy Spawn (Milestone)")]
        [ShowIf("@this.overrideMapSpawn && this.spawnFlow == GameControl.SO.MilestoneSpawnFlow.ThenMapMode && this.spawnHandOver == GameControl.SO.MilestoneHandOver.AtTime")]
        [Unit(Units.Second), MinValue(0f)]
        [LabelText("Hand Over At"), LabelWidth(175)]
        [Tooltip("วินาทีที่ส่งต่อ (นับจากเริ่มรอบเกม)\nกฎของ milestone หยุด แล้วโหมดของแมพเริ่มทำงาน\nเช่น 120 = ช่วง milestone 2 นาทีแรก")]
        public float spawnHandOverAt = 120f;

        [FoldoutGroup("Enemy Spawn (Milestone)")]
        [ShowIf("@this.overrideMapSpawn && this.spawnFlow == GameControl.SO.MilestoneSpawnFlow.ThenMapMode")]
        [LabelText("Map Timeline Starts At Hand-Over"), LabelWidth(235)]
        [Tooltip("เปิด (แนะนำ) = นาฬิกาของแมพเริ่มนับ 0 ตอนส่งต่อ Sequential ของแมพจะเล่นครบตั้งแต่ entry แรก\n"
                 + "ปิด = ใช้เวลาจริงของเกม ส่วนของแมพที่ควรเกิดก่อนส่งต่อจะถูกข้ามไป")]
        public bool spawnMapTimelineStartsAtHandOver = true;

        [FoldoutGroup("Enemy Spawn (Milestone)")]
        [ShowIf(nameof(overrideMapSpawn))]
        [GUIColor("@this.spawnMixWithConditions ? Color.green : Color.red")]
        [LabelText("Mix With Conditions"), LabelWidth(175)]
        [Tooltip("เปิด = ตอนกฎของ milestone ทำงาน ให้ตัวสุ่มเดิมทำงานคู่ไปด้วย\nปิด = เกิดจากกฎของ milestone อย่างเดียว\n"
                 + "(ใช้เฉพาะช่วงของ milestone หลังส่งต่อจะใช้ค่า Mix ของแมพ)")]
        public bool spawnMixWithConditions;

        [FoldoutGroup("Enemy Spawn (Milestone)")]
        [ShowIf("@this.overrideMapSpawn && this.spawnMixWithConditions")]
        [LabelText("Random Spawner Pool"), LabelWidth(175)]
        [Tooltip("ตอนเปิด Mix: ตัวสุ่มเดิมหยิบศัตรูตัวไหนได้บ้าง\n"
                 + "All Map Enemies = ทุกตัว\nOnly Scheduled Enemies = เฉพาะตัวที่อยู่ในกฎ\n"
                 + "Exclude Scheduled Enemies = ยกเว้นตัวที่อยู่ในกฎ (ให้กฎคุมตัวพวกนั้นเอง)")]
        public GameControl.SO.RandomSpawnerPool spawnRandomSpawnerPool;

        [FoldoutGroup("Enemy Spawn (Milestone)")]
        [ShowIf(nameof(overrideMapSpawn))]
        [InfoBox("Pattern (และสกิล Wornhole) สุ่มศัตรูได้ทุกตัวในแมพ ศัตรูที่ไม่อยู่ใน Spawn Rules ก็เกิดได้ (เช่น Shooter)", InfoMessageType.None,
            "@this.overrideMapSpawn && this.spawnPatternEnemyPool == GameControl.SO.PatternEnemyPool.AllMapEnemies")]
        [InfoBox("Pattern (และสกิล Wornhole) หยิบได้เฉพาะศัตรูที่อยู่ใน Spawn Rules", InfoMessageType.None,
            "@this.overrideMapSpawn && this.spawnPatternEnemyPool == GameControl.SO.PatternEnemyPool.OnlyScheduledEnemies")]
        [InfoBox("Pattern ไม่เกิดศัตรูเลยตอนกฎของ milestone ทำงาน", InfoMessageType.None,
            "@this.overrideMapSpawn && this.spawnPatternEnemyPool == GameControl.SO.PatternEnemyPool.NoEnemies")]
        [LabelText("Pattern Enemy Pool"), LabelWidth(175)]
        [Tooltip("ตอนกฎของ milestone ทำงาน: Enemy Pattern (รวมถึงสกิล Wornhole ของ Bright2) หยิบศัตรูตัวไหนได้บ้าง\n"
                 + "All Map Enemies = ทุกตัวในแมพเหมือนเดิม\nOnly Scheduled Enemies = เฉพาะตัวที่อยู่ใน Spawn Rules\n"
                 + "No Enemies = Pattern ไม่เกิดศัตรูเลย\n(หลังส่งต่อให้แมพ จะใช้ค่าของแมพ)")]
        public GameControl.SO.PatternEnemyPool spawnPatternEnemyPool;

        [FoldoutGroup("Enemy Spawn (Milestone)")]
        [ShowIf(nameof(overrideMapSpawn))]
        [LabelText("Spawn Rules")]
        [ListDrawerSettings(ShowPaging = false)]
        [Tooltip("กฎการเกิด 1 ข้อ = ศัตรู 1 ชนิด แต่ละข้อมีนาฬิกาของตัวเองนับจากเริ่มรอบเกม\n"
                 + "ศัตรูเลือกจากแมพของ milestone นี้ ศัตรูที่ไม่อยู่ในลิสต์จะไม่ถูกกฎสั่งเกิด\n"
                 + "กด + เพื่อเพิ่มกฎ แล้วเปิด foldout เพื่อตั้งค่า")]
        public List<GameControl.SO.EnemySpawnRule> spawnRules = new();

        [FoldoutGroup("Enemy Spawn (Milestone)")]
        [ShowInInspector, ReadOnly, LabelText("Used By Map(s)")]
        [PropertyTooltip("milestone นี้เป็นของแมพไหน (mapID) หาจาก MilestoneContainer ก่อน ถ้าไม่เจอใช้ Auto Selected / Map Filter\n"
                         + "รายชื่อศัตรูใน dropdown มาจากแมพเหล่านี้")]
        private string SpawnMapIds => string.Join(", ", GameControl.SO.EnemySpawnEditorLookup.MapIdsForMilestone(this));

        [FoldoutGroup("Enemy Spawn (Milestone)")]
        [Title("Preview", "milestone นี้เกิดศัตรูอะไร ช่วงไหน (อ่านอย่างเดียว)")]
        [ShowInInspector, ReadOnly, HideLabel, MultiLineProperty(12)]
        [PropertyTooltip("ไทม์ไลน์คำนวณจากค่าด้านบน รวมจุดส่งต่อและโหมดของแมพ\nช่วงวินาที | ศัตรู xจำนวนต่อคลื่น (ระยะห่าง)")]
        private string SpawnPreview => GameControl.SO.EnemySpawnEditorLookup.Throttled(this, "spawnPreview", () => GameControl.SO.EnemySpawnScheduleResolver.BuildMilestonePreview(this,
            GameControl.SO.EnemySpawnEditorLookup.EnemyIdsForMapIds(GameControl.SO.EnemySpawnEditorLookup.MapIdsForMilestone(this)),
            GameControl.SO.EnemySpawnEditorLookup.FirstMapForMilestone(this)));

        private bool HasUsableSpawnRules => spawnRules != null && spawnRules.Any(r => r != null && r.IsUsable);

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