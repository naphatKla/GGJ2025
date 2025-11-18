using System;
using System.Collections.Generic;
using Player;
using ProjectExtensions;

namespace Manager
{
    public class ProgressionManager : AutoCreateSingleton<ProgressionManager>
    {
        public static event Action<string> OnMapUnlocked;
        public static event Action<string> OnMapLocked;

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
        }

        private PlayerData Current
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
            var p = Current;
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
            var p = Current;
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
            var p = Current;
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

        #region Utility

        /// <summary>
        /// ให้รางวัล/ปลดล็อกแบบ “ให้ครั้งเดียว” ตาม predicate เช่น: คะแนนถึง, เล่นครบ X ครั้ง
        /// ตัวอย่าง 
        /// ProgressionManager.Instance.GrantOnce(condition: p => p.HighestScore >= 100000,onGrant: p =>
        /// { ProgressionManager.Instance.UnlockMap("hard_mapvoidmetro", saveNow: false); },saveNow: true);
        /// </summary>
        public bool GrantOnce(Func<PlayerData, bool> condition, Action<PlayerData> onGrant, bool saveNow = true)
        {
            var p = Current;
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