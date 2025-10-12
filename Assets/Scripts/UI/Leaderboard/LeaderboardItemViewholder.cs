using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI.Leaderboard;
using UnityEngine;

namespace UI.Leaderboard
{
    public class LeaderboardItemViewholder : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        
        public void SetData(LeaderboardItemModel data, int index)
        {
            if (titleText)  titleText.text = $"{index}: {data.title}";
            name = $"Cell_{index}";
        }
        
        public void OnCellReturn() { }
    }
}