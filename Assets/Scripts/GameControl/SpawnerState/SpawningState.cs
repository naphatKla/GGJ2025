using GameControl.Controller;
using GameControl.Interface;
using UnityEngine;

namespace GameControl.SpawnerState
{
    public class SpawningState : ISpawnerState
    {
        private float _enemycurrentTimer;
        private float _itemcurrentTimer;
        private float _enemyCheckTimer;
        private float _itemCheckTimer;
        
        public void Enter(SpawnerStateController controller)
        {
            GameTimer.Instance.ResumeTimer();
            _enemycurrentTimer = 0;
            _itemcurrentTimer = 0;
            
            _enemyCheckTimer = 0;
            _itemCheckTimer = 0;
        }

        public void Update(SpawnerStateController controller)
        {
            _enemycurrentTimer += Time.deltaTime;
            if (_enemycurrentTimer >= _enemyCheckTimer && controller.EnemyCanSpawn())
            {
                var selectedEnemyOption = controller.EnemySpawnerController.SpawnEnemy();
                _enemycurrentTimer = 0f;
                
                _enemyCheckTimer = selectedEnemyOption.useCustomInterval
                    ? selectedEnemyOption.customInterval 
                    : controller.EnemySpawnTimer;
            }
            
            _itemcurrentTimer += Time.deltaTime;
            if (_itemcurrentTimer >= _itemCheckTimer && controller.ItemSpawnerController.CanSpawnItem())
            {
                var selectedItemOption = controller.ItemSpawnerController.SpawnItem();
                _itemcurrentTimer = 0;
                
                _itemCheckTimer = selectedItemOption.useCustomInterval
                    ? selectedItemOption.customInterval 
                    : controller.ItemSpawnTimer;
            }
        }

        public void Exit(SpawnerStateController controller) { }
    }
}
