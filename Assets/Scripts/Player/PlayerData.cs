using System;
using System.Collections.Generic;
using PermanentUpgrade;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player
{
    [Serializable]
    public class PlayerData
    {
        public const int CURRENT_SCHEMA_VERSION = 2;
        
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
        public Dictionary<string, int> totalKillDictionary = new(); // string = enemy id
        public int HighestParryUseOnRun;
        public int HighestHealOnRun;
        public Dictionary<string, int> takeDamageOnRunDictionary = new();
        public Dictionary<string, int> totalDiedDictionary = new();
        
        // Permanent Upgrade
        public Dictionary<PermanentUpgradeType, int> PermanentUpgrades =
            new Dictionary<PermanentUpgradeType, int>();

        // Progression
        public HashSet<string> UnlockedMaps = new HashSet<string>();
        public HashSet<string> UnlockedChallenges = new HashSet<string>();
        public HashSet<PermanentUpgradeType> UnlockedPermanentUpgrade = new HashSet<PermanentUpgradeType>();
        public HashSet<string> UnlockedAchievements = new HashSet<string>();
        
        // Key ที่ยังไม่เคยเปิดดู
        public HashSet<string> RedDotKeys = new();
        
        // Selected Challenge
        public HashSet<string> SelectedChallenges = new HashSet<string>();
        
        // perMap data (key = mapId)
        public Dictionary<string, MapStat> MapStats = new Dictionary<string, MapStat>();
        
        public void PostLoadInitializeAndMigrate(int targetSchemaVersion)
        {
            //null safety
            UnlockedMaps ??= new HashSet<string>();
            UnlockedChallenges ??= new HashSet<string>();
            MapStats ??= new Dictionary<string, MapStat>();
            SelectedChallenges ??= new HashSet<string>();
            PermanentUpgrades ??= new Dictionary<PermanentUpgradeType, int>();
            totalKillDictionary ??= new Dictionary<string, int>();
            takeDamageOnRunDictionary ??= new();
            totalDiedDictionary ??= new();
            UnlockedPermanentUpgrade ??= new HashSet<PermanentUpgradeType>();
            UnlockedAchievements ??= new HashSet<string>();

            //migration
            if (SchemaVersion < 2)
            {
            }
            
            if (SchemaVersion < targetSchemaVersion)
            {
                Debug.LogWarning($"[PlayerData] SchemaVersion({SchemaVersion}) < target({targetSchemaVersion}) but no migration defined.");
                SchemaVersion = targetSchemaVersion;
            }
        }
        
        /*private void Migrate_Challenge()
        {
            var challengeIdMap = new Dictionary<string, string>
            {
                { "hard_mapvoidmetro", "map_voidmetro" },
                { "old_challenge_id", "new_challenge_id" },
            };

            foreach (var kv in challengeIdMap)
            {
                if (UnlockedChallenges.Remove(kv.Key))
                    UnlockedChallenges.Add(kv.Value);
            }
        }*/
        
        /*private void Migrate_Map()
        {
            var mapIdMap = new Dictionary<string, string>
            {
                { "hard_mapvoidmetro", "map_voidmetro" },
            };

            foreach (var (oldId, newId) in mapIdMap)
            {
                if (UnlockedMaps.Remove(oldId))
                    UnlockedMaps.Add(newId);

                if (MapStats.TryGetValue(oldId, out var stat))
                {
                    if (MapStats.TryGetValue(newId, out var existing))
                    {
                        existing.TimesPlayed += stat.TimesPlayed;
                        existing.HighestScore = Math.Max(existing.HighestScore, stat.HighestScore);
                        existing.LastScore = stat.LastScore;
                    }
                    else
                    {
                        MapStats[newId] = stat;
                    }

                    MapStats.Remove(oldId);
                }
            }
        }*/

    }

    [Serializable]
    public class MapStat
    {
        public int TimesPlayed; // amount
        public int WinAmount;
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
        public static void RegisterRun(this PlayerData p, string mapId, int score, bool isWin)
        {
            var stat = p.GetOrCreateMapStat(mapId);
            if (stat == null) return;

            stat.TimesPlayed++;
            stat.LastScore = score;
            stat.WinAmount = isWin ? stat.WinAmount + 1 : stat.WinAmount;
            if (score > stat.HighestScore) stat.HighestScore = score;
        }

        public static int GetMapHighestScore(this PlayerData p, string mapId)
            => (p != null && p.MapStats.TryGetValue(mapId, out var s)) ? s.HighestScore : 0;

        public static int GetMapTimesPlayed(this PlayerData p, string mapId)
            => (p != null && p.MapStats.TryGetValue(mapId, out var s)) ? s.TimesPlayed : 0;
    }
}
