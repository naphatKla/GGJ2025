using System;
using System.Collections.Generic;
using System.Diagnostics;
using ProjectExtensions;
using UnityEngine;
using System.IO;
using PermanentUpgrade;
using Debug = UnityEngine.Debug;
using Input = UnityEngine.Input;

namespace Player
{
    public class ActiveProfileService : NonAutoCreatePersistentSingleton <ActiveProfileService>
    {
        public PlayerData CurrentProfile { get; private set; }
        public bool HasCurrent => CurrentProfile != null;
        private const string PREFS_KEY = "current_profile_id";
        
        [Header("Debug Overlay")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        [SerializeField] private bool _debugVisible = false;
        [SerializeField] private PermanentUpgradeConfig debugUpgradeConfig;

        private Vector2 _scroll;
        private Rect _windowRect = new Rect(20, 20, 440, 400);
        private GUIStyle _hdrStyle;
        private GUIStyle _kvStyle;

        public PermanentUpgradeConfig PermanentConfig => debugUpgradeConfig;
        public Action OnPermanentUpgrade;
        
        public void AutoCreate()
        {
            Debug.Log("ActiveProfileService created.");
            LoadCurrent();
        }

        public void SetCurrent(PlayerData data)
        {
            CurrentProfile = data;
            if (data != null && !string.IsNullOrEmpty(data.ProfileId))
            {
                PlayerPrefs.SetString(PREFS_KEY, data.ProfileId);
                PlayerPrefs.Save();
            }
        }

        public PlayerData LoadCurrent()
        {
            if (CurrentProfile != null) return CurrentProfile;

            //from PlayerPrefs
            var id = PlayerPrefs.GetString(PREFS_KEY, string.Empty);
            if (!string.IsNullOrEmpty(id))
            {
                var d = PlayerSaveSystem.Instance.Read(id);
                if (d != null)
                {
                    SetCurrent(d);
                    return d;
                }
            }
            return null;
        }
        
        public void SaveNow()
        {
            if (CurrentProfile == null)
            {
                Debug.LogWarning("[ActiveProfileService] SaveNow() skipped: CurrentProfile is null.");
                return;
            }
            PlayerSaveSystem.Instance.Save(CurrentProfile);
        }
        
        
        // GUI DEBUG
        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                _debugVisible = !_debugVisible;
        }

        private void InitStylesIfNeeded()
        {
            if (_hdrStyle == null)
                _hdrStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 14
                };
            if (_kvStyle == null)
                _kvStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    wordWrap = true
                };
        }

        private void OnGUI()
        {
            if (!_debugVisible) return;
            InitStylesIfNeeded();

            _windowRect = GUI.Window(0xA11CE5, _windowRect, DrawWindow, "ActiveProfile (F1 to toggle)");
        }

        private void DrawWindow(int id)
        {
            var current = CurrentProfile;

            GUILayout.BeginVertical();
            _scroll = GUILayout.BeginScrollView(_scroll);

            if (current == null)
            {
                GUILayout.Label("CurrentProfile: <null>", _hdrStyle);
                GUILayout.Space(6);

                if (GUILayout.Button("LoadCurrent()"))
                {
                    var d = LoadCurrent();
                    Debug.Log(d != null
                        ? $"[ActiveProfileService] Loaded {d.ProfileId}"
                        : "[ActiveProfileService] LoadCurrent() => null");
                }

                GUILayout.EndScrollView();
                GUILayout.EndVertical();
                GUI.DragWindow(new Rect(0, 0, 10000, 22));
                return;
            }

            // Header
            GUILayout.Label("CurrentProfile Profile", _hdrStyle);
            DrawKV("ProfileId", current.ProfileId);
            DrawKV("DisplayName", current.DisplayName);
            DrawKV("Nano Coin", current.nanoCoin.ToString());
            DrawKV("Rainbow Anergy", current.rainBowAnergy.ToString());
            DrawKV("HighestScore", current.HighestScore.ToString());
            DrawKV("LastScore", current.LastScore.ToString());
            DrawKV("TotalDamageDeal", current.TotalDamageDeal.ToString());
          
            // ───────── Total Kill Per Enemy ─────────
            GUILayout.Space(8);
            GUILayout.Label("Total Kill (per enemy)", _hdrStyle);

            if (current.totalKillDictionary != null && current.totalKillDictionary.Count > 0)
            {
                foreach (var kv in current.totalKillDictionary)
                {
                    // แบบง่าย แสดงเป็น 1 บรรทัด
                    // Example: • slime_basic : 123
                    GUILayout.Label($"• {kv.Key} : {kv.Value}", _kvStyle);

                    // ถ้าอยากจัดเป็นสองคอลัมน์ตรง ๆ ก็ใช้แบบนี้แทน:
                    /*
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(kv.Key + ":", GUILayout.Width(140));
                    GUILayout.Label(kv.Value.ToString(), _kvStyle);
                    GUILayout.EndHorizontal();
                    */
                }
            }
            else
            {
                GUILayout.Label("(empty)", _kvStyle);
            }
            
            // ───────── Take Damage Hit Per Enemy On Run ─────────
            GUILayout.Space(8);
            GUILayout.Label("Take Damage Hit (per enemy)", _hdrStyle);

            if (current.takeDamageOnRunDictionary!= null && current.takeDamageOnRunDictionary.Count > 0)
            {
                foreach (var kv in current.takeDamageOnRunDictionary)
                {
                    // แบบง่าย แสดงเป็น 1 บรรทัด
                    // Example: • slime_basic : 123
                    GUILayout.Label($"• {kv.Key} : {kv.Value}", _kvStyle);

                    // ถ้าอยากจัดเป็นสองคอลัมน์ตรง ๆ ก็ใช้แบบนี้แทน:
                    /*
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(kv.Key + ":", GUILayout.Width(140));
                    GUILayout.Label(kv.Value.ToString(), _kvStyle);
                    GUILayout.EndHorizontal();
                    */
                }
            }
            else
            {
                GUILayout.Label("(empty)", _kvStyle);
            }
            
            DrawKV("HighestHealOnRun", current.HighestHealOnRun.ToString());
            DrawKV("HighestParryUseOnRun", current.HighestParryUseOnRun.ToString());
            DrawKV("LastPlayed (unix)", current.LastPlayedUnix.ToString());
            DrawKV("LastPlayed (local)", UnixToLocalString(current.LastPlayedUnix));

            GUILayout.Space(8);
            GUILayout.Label("UnlockedMaps", _hdrStyle);
            if (current.UnlockedMaps is { Count: > 0 })
                foreach (var m in current.UnlockedMaps)
                    GUILayout.Label($"• {m}", _kvStyle);
            else
                GUILayout.Label("(empty)", _kvStyle);
            
            GUILayout.Label("UnlockedChallenge", _hdrStyle);
            if (current.UnlockedChallenges is { Count: > 0 })
                foreach (var m in current.UnlockedChallenges)
                    GUILayout.Label($"• {m}", _kvStyle);
            else
                GUILayout.Label("(empty)", _kvStyle);
            
            GUILayout.Label("Selected Challenge", _hdrStyle);
            if (current.SelectedChallenges is { Count: > 0 })
                foreach (var m in current.SelectedChallenges)
                    GUILayout.Label($"• {m}", _kvStyle);
            else
                GUILayout.Label("(empty)", _kvStyle);
            
            GUILayout.Label("Unlocked Achievement", _hdrStyle);
            if (current.UnlockedAchievements is { Count: > 0 })
                foreach (var m in current.UnlockedAchievements)
                    GUILayout.Label($"• {m}", _kvStyle);
            else
                GUILayout.Label("(empty)", _kvStyle);

            GUILayout.Space(8);
            GUILayout.Label("MapStats", _hdrStyle);
            if (current.MapStats is { Count: > 0 })
                foreach (var kv in current.MapStats)
                {
                    var s = kv.Value;
                    GUILayout.Label($"• {kv.Key}  |  Times: {s.TimesPlayed} WIN: {s.WinAmount}  HS: {s.HighestScore}  Last: {s.LastScore}",
                        _kvStyle);
                }
            else
                GUILayout.Label("(empty)", _kvStyle);

            // ───────────────── Permanent Upgrade Debug ─────────────────
            GUILayout.Space(8);
            GUILayout.Label("Permanent Upgrades", _hdrStyle);
            current.PermanentUpgrades ??= new Dictionary<PermanentUpgradeType, int>();

            if (debugUpgradeConfig == null)
                GUILayout.Label("(PermanentUpgradeConfig not assigned)", _kvStyle);
            else
                // ไล่ทุก PermanentUpgradeType ที่เกมรองรับ
                foreach (PermanentUpgradeType type in Enum.GetValues(typeof(PermanentUpgradeType)))
                {
                    var entry = debugUpgradeConfig.GetEntry(type);
                    if (entry == null) continue;

                    var level = current.GetPermanentUpgradeLevel(type);
                    var maxLv = entry.MaxLevel;
                    var nextLv = level + 1;
                    var nextData = entry.GetLevelData(nextLv);

                    GUILayout.BeginHorizontal();
                    
                    GUILayout.Label($"• {type}: Lv {level}/{maxLv}", _kvStyle);

                    if (nextData != null)
                    {
                        GUILayout.Label($"Next Cost: {nextData.nanoCost}", GUILayout.Width(110f));

                        var canUpgrade = current.CanUpgrade(type, debugUpgradeConfig);
                        var prevEnabled = GUI.enabled;
                        GUI.enabled = canUpgrade;

                        if (GUILayout.Button("Upgrade", GUILayout.Width(80f)))
                            if (current.TryUpgrade(type, debugUpgradeConfig))
                            {
                                OnPermanentUpgrade?.Invoke();
                                SaveNow();
                                Debug.Log($"[ActiveProfileService] Debug Upgrade {type} => Lv {current.GetPermanentUpgradeLevel(type)}, Nano={current.nanoCoin}");
                            }

                        GUI.enabled = prevEnabled;
                    }
                    else
                    {
                        GUILayout.Label("(MAX)", GUILayout.Width(80f));
                    }

                    GUILayout.EndHorizontal();
                }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+100 Nano"))
            {
                OnPermanentUpgrade?.Invoke();
                current.nanoCoin += 100;
                Debug.Log($"[ActiveProfileService] Debug: +100 Nano Coin => {current.nanoCoin}");
            }

            if (GUILayout.Button("+10000 Nano"))
            {
                OnPermanentUpgrade?.Invoke();
                current.nanoCoin += 10000;
                Debug.Log($"[ActiveProfileService] Debug: +10000 Nano Coin => {current.nanoCoin}");
            }

            if (GUILayout.Button("Reset Permanent Upgrades"))
            {
                OnPermanentUpgrade?.Invoke();
                current.PermanentUpgrades.Clear();
                Debug.Log("[ActiveProfileService] Debug: PermanentUpgrades cleared.");
            }

            GUILayout.EndHorizontal();


            GUILayout.Space(10);
            GUILayout.Label("Actions", _hdrStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("SaveNow"))
            {
                SaveNow();
                Debug.Log("[ActiveProfileService] SaveNow() called.");
            }

            if (GUILayout.Button("Reload"))
                if (!string.IsNullOrEmpty(current.ProfileId))
                {
                    var re = PlayerSaveSystem.Instance.Read(current.ProfileId);
                    if (re != null) SetCurrent(re);
                }

            if (GUILayout.Button("Clear CurrentProfile"))
            {
                SetCurrent(null);
                Debug.Log("[ActiveProfileService] CurrentProfile cleared.");
            }
            
            GUILayout.Space(1);
            if (GUILayout.Button("Open Save Folder"))
            {
                var root = PlayerSaveSystem.Instance.GetRootPath();
                OpenFolderWindows(root);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, 10000, 18));
        }
        
        private static void OpenFolderWindows(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return;
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                //EditorUtility.RevealInFinder(path);
                Process.Start(path);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ActiveProfileService] OpenFolderWindows failed: {e}");
            }
        }

        private static string UnixToLocalString(long unix)
        {
            try
            {
                var dt = DateTimeOffset.FromUnixTimeSeconds(unix).ToLocalTime();
                return dt.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch
            {
                return "-";
            }
        }

        private void DrawKV(string k, string v)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(k + ":", GUILayout.Width(140));
            GUILayout.Label(v ?? "<null>", _kvStyle);
            GUILayout.EndHorizontal();
        }
    }
}
