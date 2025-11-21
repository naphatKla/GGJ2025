using System;
using System.Collections.Generic;
using PermanentUpgrade;

namespace Player
{
    [Serializable]
    public class PlayerData
    {
        public int SchemaVersion;
        
        public string ProfileId;
        public string DisplayName;
        public long LastPlayedUnix;
        
        // Currency
        public int nanoCoin;
        public int rainBowAnergy;

        // Global
        public int HighestScore;
        public int LastScore;
        public int TotalDamageDeal;
        public Dictionary<string, int> TotalKill = new(); // string = enemy id
        
        // Permanent Upgrade
        public Dictionary<PermanentUpgradeType, int> PermanentUpgrades =
            new Dictionary<PermanentUpgradeType, int>();

        // Progression
        public HashSet<string> UnlockedMaps = new HashSet<string>();
        public HashSet<string> UnlockedChallenges = new HashSet<string>();
        public HashSet<PermanentUpgradeType> UnlockedPermanentUpgrade = new HashSet<PermanentUpgradeType>();
        public HashSet<string> UnlockedAchievements = new HashSet<string>();
        
        // Selected Challenge
        public HashSet<string> SelectedChallenges = new HashSet<string>();
        
        // perMap data (key = mapId)
        public Dictionary<string, MapStat> MapStats = new Dictionary<string, MapStat>();
        
        public void PostLoadInitializeAndMigrate()
        {
            UnlockedMaps ??= new HashSet<string>();
            UnlockedChallenges ??= new HashSet<string>();
            MapStats ??= new Dictionary<string, MapStat>();
            SelectedChallenges ??= new HashSet<string>();
            PermanentUpgrades ??= new Dictionary<PermanentUpgradeType, int>();
            TotalKill ??= new Dictionary<string, int>();
            UnlockedPermanentUpgrade ??= new HashSet<PermanentUpgradeType>();
            UnlockedAchievements ??= new HashSet<string>();

            Migrate_MapId_Renames();
            const int CURRENT = 2;
            SchemaVersion = CURRENT;
        }

        /// <summary>
        /// รวม migration ที่เกี่ยวกับการ rename map id ทั้งหมด
        /// </summary>
        private void Migrate_MapId_Renames()
        {
            var mapIdMap = new Dictionary<string, string>
            {
                { "hard_mapvoidmetro", "map_voidmetro" }
                // { "old_id_2", "new_id_2" },
            };

            foreach (var kv in mapIdMap) RenameMapId(kv.Key, kv.Value);
        }

        /// <summary>
        /// ย้ายข้อมูลจาก oldId → newId ในทั้ง UnlockedMaps และ MapStats
        /// </summary>
        private void RenameMapId(string oldId, string newId)
        {
            if (string.IsNullOrEmpty(oldId) || string.IsNullOrEmpty(newId) || oldId == newId)
                return;

            // 1) UnlockedMaps : ถ้ามี id เก่าอยู่ ให้ลบแล้วใส่ id ใหม่
            if (UnlockedMaps.Remove(oldId)) UnlockedMaps.Add(newId);

            // 2) MapStats : ย้ายค่า stat จาก key เก่าไป key ใหม่
            if (MapStats.TryGetValue(oldId, out var oldStat))
            {
                if (MapStats.TryGetValue(newId, out var existing))
                {
                    existing.TimesPlayed += oldStat.TimesPlayed;
                    existing.LastScore = Math.Max(existing.LastScore, oldStat.LastScore);
                    existing.HighestScore = Math.Max(existing.HighestScore, oldStat.HighestScore);

                    MapStats[newId] = existing;
                }
                else
                {
                    MapStats[newId] = oldStat;
                }

                MapStats.Remove(oldId);
            }
        }
    }

    [Serializable]
    public class MapStat
    {
        public int TimesPlayed;
        public int HighestScore;
        public int LastScore;
    }
    
    public static class PlayerDataExtensions
    {
        public static MapStat GetOrCreateMapStat(this PlayerData p, string mapId)
        {
            if (p == null || string.IsNullOrEmpty(mapId)) return null;
            if (!p.MapStats.TryGetValue(mapId, out var stat))
            {
                stat = new MapStat();
                p.MapStats[mapId] = stat;
            }
            return stat;
        }

        /// <summary>อัปเดตสถิติเมื่อเล่นจบ 1 รอบในแผนที่ที่กำหนด</summary>
        public static void RegisterRun(this PlayerData p, string mapId, int score)
        {
            var stat = p.GetOrCreateMapStat(mapId);
            if (stat == null) return;

            stat.TimesPlayed++;
            stat.LastScore = score;
            if (score > stat.HighestScore) stat.HighestScore = score;
        }

        public static int GetMapHighestScore(this PlayerData p, string mapId)
            => (p != null && p.MapStats.TryGetValue(mapId, out var s)) ? s.HighestScore : 0;

        public static int GetMapTimesPlayed(this PlayerData p, string mapId)
            => (p != null && p.MapStats.TryGetValue(mapId, out var s)) ? s.TimesPlayed : 0;
    }
}
