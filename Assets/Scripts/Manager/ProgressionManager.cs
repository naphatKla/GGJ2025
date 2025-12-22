using System;
using System.Collections.Generic;
using PermanentUpgrade;
using Player;
using ProjectExtensions;
using UI.DotNotify;
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
        public static event Action<PermanentUpgradeType> OnPermanentUnlocked;
        
        private PlayerData CurrentPlayerData
        {
            get
            {
                var svc = ActiveProfileService.Instance;
                if (svc == null) return null;
                return svc.CurrentProfile ?? svc.LoadCurrent();
            }
        }
        
        #region Dot Notify
        private void AddRedDot(string key)
        {
            RedDotService.Instance.Add(key);
        }
        #endregion

        #region Map
        /// <summary>ปลดล็อกแมพ (คืนค่า true ถ้ามีการเปลี่ยนแปลงจริง)</summary>
        public bool UnlockMap(string mapId, bool saveNow = true, bool silent = false)
        {
            var p = CurrentPlayerData;
            if (p == null || string.IsNullOrEmpty(mapId)) return false;

            if (p.UnlockedMaps.Add(mapId))
            {
                if (saveNow) ActiveProfileService.Instance.SaveNow();
                AddRedDot($"Map:{mapId}");
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
                    AddRedDot($"Map:{id}");
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
                AddRedDot($"Challenge:{challengeId}");
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
            
            AddRedDot($"Achievement:{achievementId}");
            if (!silent)
                OnAchievementUnlocked?.Invoke(achievementId);

            Debug.Log(achievementId);
            return true;
        }

        #endregion
        
        #region Permanent Upgrade
        
        public int UnlockMultiPermanents(IEnumerable<PermanentUpgradeType> permanentType, bool saveNow = true, bool silent = false)
        {
            var p = CurrentPlayerData;
            if (p == null || permanentType == null) return 0;

            var count = 0;
            foreach (var type in permanentType)
                if (p.UnlockedPermanentUpgrade.Add(type))
                {
                    count++;
                    AddRedDot($"Permanent:{type}");
                    if (!silent) OnPermanentUnlocked?.Invoke(type);
                }

            if (count > 0 && saveNow) ActiveProfileService.Instance.SaveNow();
            return count;
        }
        
        public bool UnlockPermanentUpgrade(PermanentUpgradeType type, bool saveNow = true)
        {
            var p = CurrentPlayerData;
            if (p == null) return false;

            if (p.UnlockedPermanentUpgrade.Add(type))
            {
                AddRedDot($"Permanent:{type}");
                if (saveNow) ActiveProfileService.Instance.SaveNow();
                return true;
            }
            return false;
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
