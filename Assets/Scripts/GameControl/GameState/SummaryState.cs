using System;
using Characters.Controllers;
using Dan.Main;
using GameControl.Controller;
using GameControl.Interface;
using Player;
using UI;
using UI.Leaderboard;
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
            SpawnerStateController.Instance.ClearPatternAsync();
            SpawnerStateController.Instance.SetState(new SpawnerState.StopState());
            SpawnerStateController.Instance.ClearEnemy();
            SpawnerStateController.Instance.ClearItem();
            GameTimer.Instance.UpdateUIText();
            UIManager.Instance.CloseAllPanels();
            UIManager.Instance.OpenResultMenu();
            SavePlayerDataAndUpload();
        }

        public void Update(GameStateController controller) { }

        public void Exit(GameStateController controller) { }
        
        private void SavePlayerDataAndUpload()
        {
            var svc = ActiveProfileService.Instance;
            var profile = svc?.CurrentProfile;
            if (profile == null) return;
            
            var dataStatus = PlayerController.Instance.GetSummaryStatsOnStateEnd();
            int newScore = Mathf.Max(0, dataStatus.totalScore);
            
            profile.LastScore = newScore;
            profile.LastPlayedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            profile.RegisterRun(GameStateController.Instance.CurrentMap.mapId, newScore);
            
            bool isNewHigh = newScore > profile.HighestScore;
            if (isNewHigh)
            {
                profile.HighestScore = newScore;
            } 
            //Exchange 10% of score to currency
            var currency = (newScore / 100) * 10;
            profile.nanoCoin += currency;
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
                        LeaderboardItemPresenter.RefreshAll();
                    },
                    error =>
                    {
                        Debug.LogError($"[Leaderboard] Upload failed: {error}");
                        LeaderboardItemPresenter.RefreshAll();
                    }
                );
                
            }
        }
    }
}