using GameControl.Controller;
using GameControl.Interface;
using UnityEngine;

namespace GameControl.SpawnerState
{
    public class SpawningState : ISpawnerState
    {
        private float _currentTimer;
        private float _checkTimer;
        
        public void Enter(SpawnerStateController controller)
        {
            GameTimer.Instance.ResumeTimer();
            _currentTimer = 0;
            _checkTimer = 0;
        }

        public void Update(SpawnerStateController controller)
        {
            _currentTimer += Time.deltaTime;
            if (_currentTimer >= _checkTimer && controller.CanSpawn())
            {
                var selectedEnemyOption = controller.EnemySpawnerController.SpawnEnemy();
                _currentTimer = 0f;
                
                _checkTimer = selectedEnemyOption.useCustomInterval
                    ? selectedEnemyOption.customInterval 
                    : controller.SpawnTimer;
            }
        }

        public void Exit(SpawnerStateController controller) { }
    }
}
