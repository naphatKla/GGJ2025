using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Player
{
    /// <summary>
    /// ตัวจัดการโปรไฟล์ที่อยู่ข้ามซีน + มี Debug Overlay
    /// </summary>
    public class ActiveProfileService : MonoBehaviour
    {
        public static ActiveProfileService Instance { get; private set; }

        [Header("Debug Overlay")]
        public bool showOverlay = true;
        public KeyCode toggleKey = KeyCode.BackQuote;
        
        [Header("Defaults (for Reset)")]
        public DefaultPlayerDataSO defaultPlayerData;

        public PlayerData Current { get; private set; }
        bool _confirmReset;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFromPrefs();
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey)) showOverlay = !showOverlay;
        }

        public void InitializeFromPrefs()
        {
            var id = PlayerProfileManager.GetActiveProfileId();
            if (!string.IsNullOrEmpty(id))
                Current = PlayerSaveSystem.Read(id);
            else
                Current = null;
        }

        public void SetActive(string profileId)
        {
            if (string.IsNullOrEmpty(profileId))
            {
                Debug.LogWarning("[ActiveProfileService] profileId is empty");
                return;
            }

            var loaded = PlayerSaveSystem.Read(profileId);
            if (loaded == null)
            {
                Debug.LogWarning($"[ActiveProfileService] no save found for id={profileId}");
                return;
            }

            Current = loaded;
            PlayerProfileManager.SetActiveProfile(profileId);
            Debug.Log($"[ActiveProfileService] Active profile set to {profileId} ({Current.DisplayName})");
        }
        
        public void SaveNow()
        {
            if (Current == null || string.IsNullOrEmpty(Current.ProfileId))
            {
                Debug.LogWarning("[ActiveProfileService] No current profile to save.");
                return;
            }
            PlayerSaveSystem.WriteAtomic(Current);
            Debug.Log("[ActiveProfileService] Saved.");
        }

        public void ReloadFromDisk()
        {
            if (Current == null) { InitializeFromPrefs(); return; }
            var re = PlayerSaveSystem.Read(Current.ProfileId);
            if (re != null) Current = re;
        }

        public static string GetProfileFolder(string profileId)
        {
            var root = Path.Combine(Application.persistentDataPath, "profiles");
            return Path.Combine(root, profileId);
        }
        
        public void ResetCurrentProfile(bool keepSameId = true, string displayNameOverride = null)
        {
            if (defaultPlayerData == null)
            {
                Debug.LogError("[ActiveProfileService] Please assign DefaultPlayerDataSO to use Reset.");
                return;
            }

            string targetId;
            if (Current == null || string.IsNullOrEmpty(Current.ProfileId))
            {
                targetId =Guid.NewGuid().ToString("N");
                Current  = defaultPlayerData.Build(targetId, displayNameOverride);
            }
            else
            {
                targetId = keepSameId ? Current.ProfileId : System.Guid.NewGuid().ToString("N");

                // ลบโฟลเดอร์เก่าถ้าไม่ reuse id (หรืออยากล้างไฟล์ทั้งหมดแม้ reuse ก็ลบ/สร้างใหม่ได้)
                var folder = GetProfileFolder(Current.ProfileId);
                try
                {
                    if (Directory.Exists(folder))
                        Directory.Delete(folder, recursive: true);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ActiveProfileService] Reset delete failed: {e.Message}");
                }

                Current = defaultPlayerData.Build(targetId, displayNameOverride ?? Current.DisplayName);
            }

            PlayerProfileManager.SetActiveProfile(targetId);
            PlayerSaveSystem.WriteAtomic(Current);
            Debug.Log($"[ActiveProfileService] Profile reset. Using id={targetId}");
        }

        void OnGUI()
        {
            if (!showOverlay) return;

            const int pad = 8;
            var w = 560;
            var h = 300;
            var rect = new Rect(pad, pad, w, h);
            GUILayout.BeginArea(rect, GUI.skin.box);

            GUILayout.Label("<b>Active Profile (Debug)</b>");
            if (Current == null)
            {
                GUILayout.Label("No active profile");
                if (GUILayout.Button("Reload From Prefs")) InitializeFromPrefs();
                GUILayout.EndArea();
                return;
            }

            string shortId = string.IsNullOrEmpty(Current.ProfileId) ? "-" :
                (Current.ProfileId.Length > 8 ? Current.ProfileId.Substring(0, 8) : Current.ProfileId);

            GUILayout.Label($"Name: {Current.DisplayName}");
            GUILayout.Label($"ProfileId: {shortId}");
            GUILayout.Label($"HighestScore: {Current.HighestScore}");
            GUILayout.Label($"LastScore: {Current.LastScore}");
            if (Current.LastPlayedUnix > 0)
            {
                var dt = DateTimeOffset.FromUnixTimeSeconds(Current.LastPlayedUnix)
                                        .ToLocalTime().DateTime;
                GUILayout.Label($"LastPlayed: {dt}");
            }

            var folder = GetProfileFolder(Current.ProfileId);
            GUILayout.Label($"Path: {folder}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Folder")) Application.OpenURL("file://" + folder);
            if (GUILayout.Button("Save Now")) SaveNow();
            if (GUILayout.Button("Reload")) ReloadFromDisk();
            if (GUILayout.Button("Close Overlay")) showOverlay = false;
            GUILayout.EndHorizontal();
            
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUI.enabled = defaultPlayerData != null;
            if (_confirmReset)
            {
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Confirm Reset (wipe & defaults)"))
                {
                    ResetCurrentProfile(keepSameId: true);
                    _confirmReset = false;
                    GUI.backgroundColor = Color.white;
                }
                GUI.backgroundColor = Color.white;
                if (GUILayout.Button("Cancel")) _confirmReset = false;
            }
            else
            {
                if (GUILayout.Button("Reset Profile to Defaults")) _confirmReset = true;
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            
            GUILayout.EndArea();
        }
    }
}
