using TMPro;
using Tools;
using UnityEngine;

namespace UI.Leaderboard
{
    public class LeaderboardItemViewholder : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text scoreText;
        
        public void SetData(LeaderboardItemModel data, int index)
        {
            if (titleText)  titleText.text = $"{index}. {data.title}";
            if (scoreText)  scoreText.text = NumberAbbrev.FormatAbbrev(data.score); 
            name = $"Cell_{index}";
        }
        
        public void OnCellReturn() { }
    }
}