using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using PermanentUpgrade;
using ProjectExtensions;
using UnityEngine;

namespace Player
{
    public class PlayerSaveSystem : AutoCreateSingleton<PlayerSaveSystem>
    {
        private int SchemaVersion = 1;
        //PersistentDataPath
        [SerializeField] private string folderName = "PlayerSaves";
        //Serializer (Json.NET)
        private static readonly JsonSerializerSettings s_settings = new JsonSerializerSettings
        {
            Formatting = Formatting.None, 
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        };


        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
            EnsureRootExists();
        }
        
        public void AutoCreate()
        {
            Debug.Log("PlayerSaveSystem created.");
        }

        private string RootPath => Path.Combine(Application.persistentDataPath, folderName);

        private void EnsureRootExists()
        {
            if (!Directory.Exists(RootPath))
                Directory.CreateDirectory(RootPath);
        }

        private string GetFilePath(string profileId)
        {
            var safe = MakeFileSafe(profileId);
            return Path.Combine(RootPath, $"{safe}.json");
        }

        private static string MakeFileSafe(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return string.IsNullOrWhiteSpace(name) ? "profile" : name;
        }

        // ----- Public API -----

        /// <summary>NEW PROFILE</summary>
        public PlayerData CreateNew(string displayName)
        {
            EnsureRootExists();
            var id = Guid.NewGuid().ToString("N");
            var data = new PlayerData
            {
                SchemaVersion = SchemaVersion,
                ProfileId = id,
                DisplayName = displayName ?? "Player",
                LastPlayedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                nanoCoin = 0,
                rainBowAnergy = 0,
                HighestScore = 0,
                LastScore = 0,
                PermanentUpgrades = new Dictionary<PermanentUpgradeType, int>(),
                UnlockedMaps = new HashSet<string>(),
                UnlockedChallenges = new HashSet<string>(),
                MapStats = new Dictionary<string, MapStat>()
            };
            Save(data);
            return data;
        }

        /// <summary>SAVE PROFILE</summary>
        public void Save(PlayerData data)
        {
            if (data == null || string.IsNullOrEmpty(data.ProfileId))
            {
                Debug.LogError("[PlayerSaveSystem] Save failed: invalid data or ProfileId.");
                return;
            }

            data.SchemaVersion = SchemaVersion;
            EnsureRootExists();
            data.LastPlayedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            string json = JsonConvert.SerializeObject(data, s_settings);
            string path = GetFilePath(data.ProfileId);
            WriteAtomic(path, json);
        }

        /// <summary>READ PROFILE</summary>
        public PlayerData Read(string profileId)
        {
            if (string.IsNullOrEmpty(profileId)) return null;

            string path = GetFilePath(profileId);
            if (!File.Exists(path)) return null;

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                var data = JsonConvert.DeserializeObject<PlayerData>(json, s_settings);
                data?.PostLoadInitializeAndMigrate();
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerSaveSystem] Read failed: {e}");
                return null;
            }
        }


        public bool Exists(string profileId)
        {
            return File.Exists(GetFilePath(profileId));
        }

        public bool Delete(string profileId)
        {
            try
            {
                string path = GetFilePath(profileId);
                if (File.Exists(path))
                {
                    File.Delete(path);
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerSaveSystem] Delete failed: {e}");
            }
            return false;
        }

        /// <summary>ดึงรายชื่อโปรไฟล์ (จากไฟล์ .json ทั้งหมด)</summary>
        public List<string> ListProfileIds()
        {
            EnsureRootExists();
            if (!Directory.Exists(RootPath)) return new List<string>();
            return Directory
                .EnumerateFiles(RootPath, "*.json", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .ToList();
        }
        
        public string GetRootPath()
        {
            var folderName = this.folderName;
            return System.IO.Path.Combine(Application.persistentDataPath, folderName);
        }

        // ----- Utilities -----

        private static void WriteAtomic(string finalPath, string content)
        {
            var tempPath = finalPath + ".tmp";
            try
            {
                File.WriteAllText(tempPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                if (File.Exists(finalPath))
                {
                    File.Replace(tempPath, finalPath, null);
                }
                else
                {
                    File.Move(tempPath, finalPath);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerSaveSystem] Atomic write failed: {e}");
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch {}
            }
        }
    }
}
