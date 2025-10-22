using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Leaderboard
{
    public class LeaderboardItemModel
    {
        public string title;
        public float score;
        public LeaderboardItemModel(string t,float s)
        {
            title = t;
            score = s;
        }
    }

}
