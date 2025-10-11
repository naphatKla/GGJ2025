using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Player
{
    public class PlayerProfileManager : MonoBehaviour
    {
        const string LastKey = "lastActiveProfileId";
        public static string CreateNew(string displayName, PlayerData defaults = null)
        {
            var id = System.Guid.NewGuid().ToString("N");
            var data = defaults ?? new PlayerData();
            data.ProfileId   = id;
            data.DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Player" : displayName;
            data.SaveVersion = 1;

            PlayerSaveSystem.WriteAtomic(data);
            PlayerPrefs.SetString(LastKey, id);
            PlayerPrefs.Save();
            return id;
        }

        public static PlayerData ContinueOrNull()
        {
            var id = PlayerPrefs.GetString(LastKey, "");
            if (string.IsNullOrEmpty(id)) return null;
            return PlayerSaveSystem.Read(id);
        }

        public static void SetActiveProfile(string profileId)
        {
            PlayerPrefs.SetString(LastKey, profileId);
            PlayerPrefs.Save();
        }

        public static string GetActiveProfileId() => PlayerPrefs.GetString(LastKey, "");
        
        
        public static void ResetCurrentProfile(PlayerData defaults)
        {
            var id = GetActiveProfileId();
            if (string.IsNullOrEmpty(id)) 
                id = CreateNew(defaults?.DisplayName ?? "Player", defaults);
            else
            {
                var fresh = defaults ?? new PlayerData();
                fresh.ProfileId = id;
                fresh.DisplayName = string.IsNullOrWhiteSpace(fresh.DisplayName) ? "Player" : fresh.DisplayName;
                PlayerSaveSystem.WriteAtomic(fresh);
            }
        }
    }
}