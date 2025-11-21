using System;
using System.Collections.Generic;
using Player;
using ProjectExtensions;
using UI.Manager;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manager
{
    public class ProgressionManager : AutoCreatePersistentSingleton<ProgressionManager>
    {
        public static event Action<string> OnMapUnlocked;
        public static event Action<string> OnMapLocked;
        public static event Action<string> OnChallengeUnlocked; 
        public static event Action<string> OnAchievementUnlocked;
        
        private PlayerData CurrentPlayerData
        {
            get
            {
                var svc = ActiveProfileService.Instance;
                if (svc == null) return null;
                return svc.CurrentProfile ?? svc.LoadCurrent();
            }
        }

        #region Map
        /// <summary>ปลดล็อกแมพ (คืนค่า true ถ้ามีการเปลี่ยนแปลงจริง)</summary>
        public bool UnlockMap(string mapId, bool saveNow = true, bool silent = false)
        {
            var p = CurrentPlayerData;
            if (p == null || string.IsNullOrEmpty(mapId)) return false;

            if (p.UnlockedMaps.Add(mapId))
            {
                if (saveNow) ActiveProfileService.Instance.SaveNow();
                if (!silent) OnMapUnlocked?.Invoke(mapId);
                return true;
            }
            return false;
        }

        /// <summary>ล็อกแมพกลับ (สำหรับดีบัก/รีเซ็ต)</summary>
        public bool LockMap(string mapId, bool saveNow = true, bool silent = false)
        {
            var p = CurrentPlayerData;
            if (p == null || string.IsNullOrEmpty(mapId)) return false;

            if (p.UnlockedMaps.Remove(mapId))
            {
                if (saveNow) ActiveProfileService.Instance.SaveNow();
                if (!silent) OnMapLocked?.Invoke(mapId);
                return true;
            }
            return false;
        }
        
        /// <summary>ปลดล็อกหลายแมพรวดเดียว</summary>
        public int UnlockMaps(IEnumerable<string> mapIds, bool saveNow = true, bool silent = false)
        {
            var p = CurrentPlayerData;
            if (p == null || mapIds == null) return 0;

            var count = 0;
            foreach (var id in mapIds)
                if (!string.IsNullOrEmpty(id) && p.UnlockedMaps.Add(id))
                {
                    count++;
                    if (!silent) OnMapUnlocked?.Invoke(id);
                }

            if (count > 0 && saveNow) ActiveProfileService.Instance.SaveNow();
            return count;
        }
        
        #endregion

        #region Challenge

        public bool UnlockChallenge(string challengeId, bool saveNow = true, bool silent = false)
        {
            var p = CurrentPlayerData;
            if (p == null || string.IsNullOrEmpty(challengeId)) return false;

            if (p.UnlockedChallenges.Add(challengeId))
            {
                if (saveNow) ActiveProfileService.Instance.SaveNow();
                if (!silent) OnChallengeUnlocked?.Invoke(challengeId);
                return true;
            }
            return false;
        }

        #endregion
        
        #region Achievement

        public bool IsAchievementUnlocked(string achievementId)
        {
            var p = CurrentPlayerData;
            if (p == null || string.IsNullOrEmpty(achievementId)) return false;
            p.UnlockedAchievements ??= new HashSet<string>();
            return p.UnlockedAchievements.Contains(achievementId);
        }

        public bool UnlockAchievement(
            string achievementId,
            Action<PlayerData> onReward = null,
            bool saveNow = true,
            bool silent = false)
        {
            var p = CurrentPlayerData;
            if (p == null || string.IsNullOrEmpty(achievementId)) return false;

            p.UnlockedAchievements ??= new HashSet<string>();

            if (!p.UnlockedAchievements.Add(achievementId))
                return false;

            onReward?.Invoke(p);

            if (saveNow)
                ActiveProfileService.Instance.SaveNow();

            if (!silent)
                OnAchievementUnlocked?.Invoke(achievementId);

            Debug.Log(achievementId);
            return true;
        }

        #endregion

        #region Utility

        public bool GrantOnce(Func<PlayerData, bool> condition, Action<PlayerData> onGrant, bool saveNow = true)
        {
            var p = CurrentPlayerData;
            if (p == null || condition == null || onGrant == null) return false;
            if (condition(p))
            {
                onGrant(p);
                if (saveNow) ActiveProfileService.Instance.SaveNow();
                return true;
            }
            return false;
        }


        #endregion
    }

}
