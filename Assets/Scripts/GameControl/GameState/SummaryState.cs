using System;
using Achievements;
using Characters.Controllers;
using Cysharp.Threading.Tasks;
using GameControl.Controller;
using GameControl.Interface;
using Manager;
using Player;
using UI;
using UI.Leaderboard;
using UnityEngine;

namespace GameControl.GameState
{
    public class SummaryState : IGameState
    {
        private bool _uploadedThisRun = false;

        public void OnEnable(GameStateController controller)
        {
        }

        public void OnDisable(GameStateController controller)
        {
        }

        public void Enter(GameStateController controller)
        {
            SpawnerStateController.Instance.ClearPatternAsync();
            SpawnerStateController.Instance.SetState(new SpawnerState.StopState());
            SpawnerStateController.Instance.ClearEnemy();
            SpawnerStateController.Instance.ClearItem();
            GameTimer.Instance.UpdateUIText();
            UIManager.Instance.OpenResultMenu();
            SavePlayerDataAndUpload(controller);

            AchievementManager.Current.OnRunEnded(controller.CurrentMap.mapId);

            switch (GameStateController.Current.gameResult)
            {
                case EndResult.Completed:
                    AchievementManager.Current.OnRunWin(controller.CurrentMap.mapId);
                    break;
            }
        }

        public void Update(GameStateController controller)
        {
        }

        public void Exit(GameStateController controller)
        {
        }

        private void SavePlayerDataAndUpload(GameStateController controller)
        {
            var svc = ActiveProfileService.Instance;
            var profile = svc?.CurrentProfile;
            if (profile == null) return;

            var dataStatus = PlayerController.Instance.GetSummaryStatsOnStateEnd();
            int newScore = Mathf.Max(0, dataStatus.totalScore);
            int damageDealThisRun = Mathf.Max(0, dataStatus.totalDamageDeal);

            profile.LastScore = newScore;
            profile.TotalDamageDeal += damageDealThisRun;

            if (dataStatus.totalSecondarySkillUsed > profile.HighestParryUseOnRun)
                profile.HighestParryUseOnRun = dataStatus.totalSecondarySkillUsed;

            if (dataStatus.totalHeal > profile.HighestHealOnRun)
                profile.HighestHealOnRun = dataStatus.totalHeal;

            // set total take damage hit from each enemy type in this run
            profile.takeDamageOnRunDictionary = dataStatus.takeDamageAmountDictionary;

            // add total died damage hit from each enemy type 
            foreach (var keyValuePair in dataStatus.diedDictionary)
            {
                profile.totalDiedDictionary.TryAdd(keyValuePair.Key, 0);
                profile.totalDiedDictionary[keyValuePair.Key]++;
            }

            // add total kill of each enemy type  
            foreach (var kv in dataStatus.totalKillDictionary)
            {
                profile.totalKillDictionary.TryAdd(kv.Key, 0);
                profile.totalKillDictionary[kv.Key] += kv.Value;
            }

            profile.LastPlayedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            bool isWin = controller.gameResult == EndResult.Completed;
            var mapStats = profile.GetOrCreateMapStat(GameStateController.Instance.CurrentMap.mapId);

            int maxLevelUnlockMilestone = Mathf.Clamp(mapStats.MaxLevelUnlockMilestone,0, GameStateController.Instance.CurrentMap.MaxMilestoneLevel);
            int calculatedNextLevelMilestone = maxLevelUnlockMilestone;
            
            if (mapStats.SelectedLevelMilestone == maxLevelUnlockMilestone)
            {
                calculatedNextLevelMilestone = GameStateController.Instance.CurrentMap.EvaluateLevelMilestoneOption(dataStatus, maxLevelUnlockMilestone, isWin);
            }
            
            profile.RegisterRun(GameStateController.Instance.CurrentMap.mapId, newScore, isWin,
                calculatedNextLevelMilestone);

            bool isNewHigh = newScore > profile.HighestScore;
            if (isNewHigh)
            {
                profile.HighestScore = newScore;
            }

            //Exchange 10% of score to currency
            var currency = (newScore / 100) * 1;
            profile.nanoCoin += currency;
            svc.SaveNow();

            // Leaderboard
            if (isNewHigh)
            {
                UploadHighScoreAsync(profile, newScore).Forget();
            }
            else if (newScore > 0)
            {
                Debug.Log($"[Leaderboard] Upload skipped: {newScore} is not higher than local high score {profile.HighestScore}");
            }
        }

        private async UniTaskVoid UploadHighScoreAsync(PlayerData profile, int score)
        {
            try
            {
                await PlayFabLeaderboardService.SubmitHighestScoreAsync(profile, score);
                Debug.Log($"[Leaderboard] PlayFab upload success: {profile.DisplayName} -> {score}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Leaderboard] PlayFab upload failed: {ex.Message}");
            }
            finally
            {
                LeaderboardItemPresenter.RefreshAll();
                LeaderboardItemPresenter.RefreshAllDelayed(1000);
                LeaderboardItemPresenter.RefreshAllDelayed(2500);
            }
        }
    }
}
