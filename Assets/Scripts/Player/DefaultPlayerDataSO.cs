using UnityEngine;

namespace Player
{
    [CreateAssetMenu(fileName = "DefaultPlayerData", menuName = "Game/Defaults/PlayerData")]
    public class DefaultPlayerDataSO : ScriptableObject
    {
        [Header("Defaults")]
        public string defaultDisplayName = "Player";
        public int defaultHighestScore = 0;
        public int defaultlastScore = 0;

        public PlayerData Build(string profileId, string displayNameOverride = null)
        {
            return new PlayerData {
                ProfileId     = profileId,
                SaveVersion   = 1,
                DisplayName   = string.IsNullOrWhiteSpace(displayNameOverride) ? defaultDisplayName : displayNameOverride,
                HighestScore  = defaultHighestScore,
                LastScore = defaultlastScore,
                LastPlayedUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }
    }
}
