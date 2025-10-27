using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameControl;
using GameControl.Controller;
using GameControl.Interface;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace GameControl.GameState
{
    public class EndState : IGameState
    {
        private CancellationTokenSource _cts;
        
        public void OnEnable(GameStateController controller) { }

        public void OnDisable(GameStateController controller) { }

        public void Enter(GameStateController controller)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            
            //Stop spawning
            GameTimer.Instance.StopTimer();
            GameTimer.Instance.ClearAllTriggers();
            GameTimer.Instance.ChangeToText("KILL ALL ENEMIES");
            SpawnerStateController.Instance.ClearPatternAsync();
            SpawnerStateController.Instance.SetState(new SpawnerState.StopState());
        }

        public void Update(GameStateController controller)
        {
            //Check all enemy dead
            if (SpawnerStateController.Instance.EnemyAmount <= 0 && controller.gameResult == EndResult.None)
            {
                GameStateController.Instance.gameResult = EndResult.Completed;
                WaitBeforeSummary(_cts.Token).Forget();
            }
        }

        public void Exit(GameStateController controller) { }
        
        private async UniTaskVoid WaitBeforeSummary(CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.5), DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, token);
                if (GameStateController.Instance.CurrentState is EndState)
                {
                    GameStateController.Instance.SetState(new SummaryState());
                }
            }
            catch (OperationCanceledException) {}
        }
    }
}