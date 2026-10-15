using TMPro;
using Tools;
using UnityEngine;

namespace UI.Leaderboard
{
    public class LeaderboardItemViewholder : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text scoreText;
        [Tooltip("สีแถวของตัวเอง")]
        [SerializeField] private Color mineColor = new Color(1f, 0.84f, 0f, 1f);

        private Color _titleDefault, _scoreDefault;
        private bool _hasDefaults;

        public void SetData(LeaderboardItemModel data, int index)
        {
            CacheDefaults();
            if (titleText)
            {
                titleText.text = data.isMine ? $"{index+1}. {data.title} (YOU)" : $"{index+1}. {data.title}";
                titleText.color = data.isMine ? mineColor : _titleDefault;
            }
            if (scoreText)
            {
                scoreText.text = NumberAbbrev.FormatAbbrev(data.score);
                scoreText.color = data.isMine ? mineColor : _scoreDefault;
            }
            name = $"Cell_{index}";
        }

        private void CacheDefaults()
        {
            if (_hasDefaults) return;
            if (titleText)
            {
                _titleDefault = titleText.color;
                // ชื่อยาวให้ตัดเป็น "..." ในบรรทัดเดียว ไม่ล้นลงบรรทัดใหม่
                titleText.enableWordWrapping = false;
                titleText.overflowMode = TextOverflowModes.Ellipsis;
            }
            if (scoreText) _scoreDefault = scoreText.color;
            _hasDefaults = true;
        }

        public void OnCellReturn() { }
    }
}
