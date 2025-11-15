using System;
using Characters.Controllers;
using Characters.SO.CharacterDataSO;
using UnityEngine;

namespace Characters.ScoreSystems
{
    public class ScoreSystem : MonoBehaviour
    {
        public int CurrentScore { get; private set; }
        public float ScoreMultiplier { get; set; } = 1;
        public event Action<int> OnScoreChange;
        public event Action<float> OnScoreMultiplierChange;
        private BaseController _owner;

        public void AssignData(BaseController owner)
        {
            _owner = owner;
        }

        public void OnKill(BaseController enemy)
        {
            EnemyDataSo dat = enemy.CharacterData as EnemyDataSo;
            AddScore(dat.ScoreDrop);
        }

        public void OnRankModify(CombatRankData oldRank, CombatRankData newRank)
        {
            AddScoreMultiplyer(-oldRank.scoreMultiplier);
            AddScoreMultiplyer(newRank.scoreMultiplier);
        }
        
        private void AddScore(int score, bool useMultiplier = true)
        {
            score = useMultiplier ? Mathf.CeilToInt(score* ScoreMultiplier) : score;
            if (score == 0) return;
            
            CurrentScore += score;
            OnScoreChange?.Invoke(CurrentScore);
        }

        public void AddScoreMultiplyer(float multiplyer)
        {
            ScoreMultiplier += multiplyer;
            OnScoreMultiplierChange?.Invoke(ScoreMultiplier);
        }

        public void ResetScoreSystem()
        {
            CurrentScore = 0;
            ScoreMultiplier = 1;
        }
    }
}
