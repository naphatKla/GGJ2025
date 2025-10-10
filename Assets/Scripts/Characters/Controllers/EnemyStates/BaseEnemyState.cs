using Characters.InputSystems;
using Characters.SO.CharacterDataSO.EnemyStateDataSO;

namespace Characters.Controllers.EnemyStates
{
    public abstract class BaseEnemyState
    {
        protected EnemyController ownerEnemyController;
        protected EnemyInputReader enemyInputReader;
        protected BaseEnemyStateDataSo stateData;
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
            isPerforming = true;
            OnEnterState();
        }
        
        public virtual void HandleOnUpdate()
        {
            OnUpdateState();
        }
        
        public virtual void HandleOnExit()
        {
            isPerforming = false;
            OnExitState();
        }
        
        protected abstract void OnEnterState();
        protected abstract void OnUpdateState();
        protected abstract void OnExitState();
    }
}
