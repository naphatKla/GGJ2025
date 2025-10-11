using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Player
{
    public class PlayerSaveSystem : MonoBehaviour
    {
        static string Root => Path.Combine(Application.persistentDataPath, "profiles");
        static string Dir(string id) => Path.Combine(Root, id);
        static string MainPath(string id) => Path.Combine(Dir(id), "player_v1.json");
        static string BakPath(string id) => Path.Combine(Dir(id), "player_v1.bak");
        static string TmpPath(string id) => Path.Combine(Dir(id), "player_v1.tmp");

        public static void WriteAtomic(PlayerData data)
        {
            if (data == null || string.IsNullOrEmpty(data.ProfileId))
            {
                Debug.LogError("[PlayerSaveSystem] Data/ProfileId is null.");
                return;
            }

            Directory.CreateDirectory(Dir(data.ProfileId));
            data.LastPlayedUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var json = JsonUtility.ToJson(data, false);
            File.WriteAllText(TmpPath(data.ProfileId), json);

            if (File.Exists(MainPath(data.ProfileId)))
            {
                File.Copy(MainPath(data.ProfileId), BakPath(data.ProfileId), overwrite: true);
                File.Delete(MainPath(data.ProfileId));
            }

            File.Move(TmpPath(data.ProfileId), MainPath(data.ProfileId));
        }

        public static PlayerData Read(string profileId)
        {
            var p = MainPath(profileId);
            if (!File.Exists(p))
            {
                var bak = BakPath(profileId);
                if (File.Exists(bak))
                {
                    File.Copy(bak, p, overwrite: true);
                }
                else
                {
                    return null;
                }
            }

            var json = File.ReadAllText(p);
            var data = JsonUtility.FromJson<PlayerData>(json);
            return data;
        }
    }

}