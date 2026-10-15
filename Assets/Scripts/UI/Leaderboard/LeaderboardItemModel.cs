using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Leaderboard
{
    public class LeaderboardItemModel
    {
        public string title;
        public float score;
        public bool isMine;
        public LeaderboardItemModel(string t,float s, bool mine = false)
        {
            title = t;
            score = s;
            isMine = mine;
        }
    }

}
