using System.Text.RegularExpressions;
using Characters.Controllers;
using Dan.Main;
using GameControl.Controller;
using GameControl.Interface;
using MoreMountains.Feedbacks;
using Player;
using UI;
using UnityEngine;

namespace GameControl.GameState
{
    public class SummaryState : IGameState
    {
        private bool _uploadedThisRun = false;
        
        public void OnEnable(GameStateController controller) { }

        public void OnDisable(GameStateController controller) { }

        public void Enter(GameStateController controller)
        {
            UIManager.Instance.CloseAllPanels();
            UIManager.Instance.OpenResultMenu();
            SavePlayerDataAndUpload();
        }

        public void Update(GameStateController controller) { }

        public void Exit(GameStateController controller) { }
        
        private void SavePlayerDataAndUpload()
        {
            var svc = ActiveProfileService.Instance;
            var profile = svc?.Current;
            if (profile == null) return;

            var dataStatus = PlayerController.Instance.GetSummaryStatsOnStateEnd();
            int newScore = Mathf.Max(0, dataStatus.totalScore);
            profile.LastScore = newScore;

            bool isNewHigh = newScore > profile.HighestScore;
            if (isNewHigh)
            {
                profile.HighestScore = newScore;
            }
            
            svc.SaveNow();
            
            // Leaderboard
            if (isNewHigh)
            {
                LeaderboardCreator.SetUserGuid(profile.ProfileId);  
                string name = $"{profile.DisplayName}";
                Leaderboards.ThailandGameShow.UploadNewEntry(
                    name, newScore,
                    e =>
                    {
                        Debug.Log($"[Leaderboard] Upload success: {name} -> {newScore}");
                    },
                    error =>
                    {
                        Debug.LogError($"[Leaderboard] Upload failed: {error}");
                    }
                );
            }
        }
    }
}