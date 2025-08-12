using System;
using Characters.Controllers;
using UnityEngine;

namespace Characters.ScoreSystems
{
    public class ScoreSystem : MonoBehaviour
    {
        public int CurrentScore { get; private set; }
        public int ScoreMultiplier { get; set; } = 1;
        public event Action<int> OnScoreChange;
        private BaseController _owner;

        public void AssignData(BaseController owner)
        {
            _owner = owner;
        }
        
        public void AddScore(int score, bool useMultiplier = true)
        {
            score = useMultiplier ? score * ScoreMultiplier : score;
            if (score == 0) return;
            
            CurrentScore += score;
            OnScoreChange?.Invoke(CurrentScore);
        }

        public void ResetScoreSystem()
        {
            CurrentScore = 0;
            ScoreMultiplier = 1;
        }
    }
}
