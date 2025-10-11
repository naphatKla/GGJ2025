using Characters.Controllers;
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
        public void OnEnable(GameStateController controller) { }

        public void OnDisable(GameStateController controller) { }

        public void Enter(GameStateController controller)
        {
            UIManager.Instance.CloseAllPanels();
            UIManager.Instance.OpenResultMenu();
            SavePlayerData();
        }

        public void Update(GameStateController controller) { }

        public void Exit(GameStateController controller) { }

        private void SavePlayerData()
        {
            var svc = ActiveProfileService.Instance;
            if (svc?.Current == null) return;
            var dataStatus = PlayerController.Instance.GetSummaryStatsOnStateEnd();
            int newScore = Mathf.Max(0, dataStatus.totalScore);
            svc.Current.LastScore = newScore;

            if (newScore > svc.Current.HighestScore)
            {
                svc.Current.HighestScore = newScore;
                svc.SaveNow();
            }
        }
    }
}