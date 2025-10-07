using System.Threading;
using Characters.InputSystems;
using Characters.SO.CharacterDataSO.EnemyStateDataSO;
using Cysharp.Threading.Tasks;

namespace Characters.Controllers.EnemyStates
{
    public abstract class BaseEnemyState
    {
        protected EnemyController ownerEnemyController;
        protected EnemyInputReader enemyInputReader;
        protected BaseEnemyStateDataSo stateData;
        protected CancellationTokenSource cts;
        protected bool isPerforming;
        public bool IsPerforming => isPerforming;
        
        public virtual void AssignData(EnemyController ownerEnemy, BaseEnemyStateDataSo data)
        {
            ownerEnemyController = ownerEnemy;
            enemyInputReader = ownerEnemyController.InputSystem as EnemyInputReader;
            stateData = data;
        }

        public virtual void HandleOnStart()
        {
            if (isPerforming) return;
            
            isPerforming = true;
            OnEnterState();
            HandleOnUpdate().Forget();
        }
        
        protected virtual async UniTask HandleOnUpdate()
        {
            cts = new CancellationTokenSource();
            await OnUpdateState(cts.Token);
            HandleOnExit();
        }
        
        protected virtual void HandleOnExit()
        {
            isPerforming = false;
            OnExitState();
            enemyInputReader.ResetStateToDefault();
        }

        public virtual void CancelState()
        {
            cts?.Cancel();
        }
        
        protected abstract void OnEnterState();
        protected abstract UniTask OnUpdateState(CancellationToken cancellationToken);
        protected abstract void OnExitState();
    }
}
