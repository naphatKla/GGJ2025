using System;
using System.Collections.Generic;

namespace Player
{
    [Serializable]
    public class PlayerData
    {
        public int SchemaVersion;
        
        public string ProfileId;
        public string DisplayName;
        public long LastPlayedUnix;

        // Global
        public int HighestScore;
        public int LastScore;

        // Progression
        public HashSet<string> UnlockedMaps = new HashSet<string>();
        public HashSet<string> UnlockedChallenges = new HashSet<string>();
        
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

            const int CURRENT = 2;
            if (SchemaVersion < 1)
            {
                // v1 → v2: เติมค่า default fields ใหม่ ฯลฯ
            }

            SchemaVersion = CURRENT;
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
