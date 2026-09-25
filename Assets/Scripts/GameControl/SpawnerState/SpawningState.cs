using Characters.Controllers;
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
            // Enemy Spawn Mode schedule. Inactive (null-safe no-op) in Conditions mode; Rush always hands
            // spawning back to the random spawner.
            var schedule = controller.SpawnSchedule;
            bool inRush = GameStateController.Instance != null && GameStateController.Instance.MapState == MapState.Rush;
            if (!inRush) schedule?.Tick();
            bool randomSpawnerOn = inRush || schedule == null || schedule.AllowsRandomSpawner;

            _enemycurrentTimer += Time.deltaTime;
            if (randomSpawnerOn && _enemycurrentTimer >= _enemyCheckTimer && controller.EnemyCanSpawn())
            {
                var selectedEnemyOption = controller.EnemySpawnerController.SpawnEnemy();
                _enemycurrentTimer = 0f;

                if (selectedEnemyOption != null)
                    _enemyCheckTimer = selectedEnemyOption.useCustomInterval
                        ? selectedEnemyOption.customInterval
                        : controller.EnemySpawnTimer;
                else
                    _enemyCheckTimer = controller.EnemySpawnTimer;
            }

            _itemcurrentTimer += Time.deltaTime;
            if (_itemcurrentTimer >= _itemCheckTimer && controller.ItemSpawnerController.CanSpawnItem())
            {
                var selectedItemOption = controller.ItemSpawnerController.SpawnItem();
                _itemcurrentTimer = 0;

                if (selectedItemOption != null)
                    _itemCheckTimer = selectedItemOption.useCustomInterval
                        ? selectedItemOption.customInterval
                        : controller.ItemSpawnTimer;
                else
                    _itemCheckTimer = controller.ItemSpawnTimer;
            }
        }


        public void Exit(SpawnerStateController controller)
        {

        }
    }
}
