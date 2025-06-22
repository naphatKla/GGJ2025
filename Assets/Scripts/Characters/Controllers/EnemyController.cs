
using System;
using GameControl;
using GameControl.Interface;
using UnityEngine;
using UnityEngine.Events;

namespace Characters.Controllers
{
    public class EnemyController : BaseController , ISpawnable , IDespawnable
    {
        public string EnemyId { get; set; }
        public float EnemyPoint { get; set; }

        public void OnSpawned()
        {
            HealthSystem.OnDead += HandleDeath;
        }

        public void OnDespawned()
        {
            HealthSystem.OnDead -= HandleDeath;
            ResetAllDependentBehavior();
        }
        
        private void HandleDeath()
        {
            PoolingSystem.Instance.Despawn(EnemyId, gameObject);
            SpawnerStateController.Instance.CurrentEnemyPoint += EnemyPoint;
        }
    }
}
