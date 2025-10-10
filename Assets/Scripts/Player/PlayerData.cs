using System;

namespace Player
{
    [Serializable]
    public class PlayerData
    {
        public string ProfileId;
        public int SaveVersion = 1;
        public string DisplayName;
        public long LastPlayedUnix;
        
        //Game Achivment
        public int HighestScore;
        
        //Game Progression
    }

}
