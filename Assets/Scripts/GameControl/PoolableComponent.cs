using System;
using Characters.CollectItemSystems;
using GameControl.Interface;
using UnityEngine;

namespace GameControl
{
    public class PoolableComponent : MonoBehaviour , ISpawnable , IDespawnable
    {
        public string ComponenetId { get; set; }
        
        public event Action<GameObject> OnDespawn;
        public event Action<GameObject> OnSpawn;

        public void OnSpawned()
        {
            OnSpawn?.Invoke(gameObject);
        }

        public void OnDespawned()
        {
            OnDespawn?.Invoke(gameObject);
        }
    }

}
