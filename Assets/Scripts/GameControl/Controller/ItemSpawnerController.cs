using System.Collections;
using System.Collections.Generic;
using GameControl.SO;
using UnityEngine;

namespace GameControl.Controller
{
    public class ItemSpawnerController
    {
        private SO.MapDataSO _mapdata;
        private SpawnerStateController _state;
        private readonly Vector2 _itemRegionSize;
        private List<SO.MapDataSO.ItemOption> _storeItem;
        private List<GameObject> _activeItem;

        public ItemSpawnerController(SO.MapDataSO mapData, SpawnerStateController state, Vector2 spawnRegion)
        {
            _mapdata = mapData;
            _state = state;
            _itemRegionSize = spawnRegion;
        }
        
        public void PrewarmItem()
        {
            _storeItem = new List<MapDataSO.ItemOption>();
            _activeItem = new List<GameObject>();
            foreach (var data in _mapdata.ItemOptions)
            {
                var cloned = new MapDataSO.ItemOption()
                {
                    id = data.id,
                    prewarmCount = data.prewarmCount,
                    useCustomInterval = data.useCustomInterval,
                    customInterval = data.customInterval
                };

                _storeItem.Add(cloned);
                foreach (var item in PoolingSystem.Instance.Prewarm(data.id, data.itemObj.gameObject, data.prewarmCount, _state.ItemParent))
                {
                    var enemyController = item.GetComponent<PoolableComponent>();
                    enemyController.ComponenetId = data.id;
                }
            }
        }
        
        public MapDataSO.ItemOption SpawnItem()
        {
            //Random
            var randomItem = RandomUtility.GetWeightedRandom(_storeItem);
            var itemObj = PoolingSystem.Instance.Spawn(randomItem.id, SpawnUtility.RandomInsideRegion(_itemRegionSize));

            _activeItem.Add(itemObj);
            
            var poolable = itemObj.GetComponent<PoolableComponent>();
            poolable.OnDespawn += ResetItemData;
            return randomItem;
        }
        
        public void ResetItemData(GameObject itemOption)
        {
            _activeItem.Remove(itemOption);
            var poolable = itemOption.GetComponent<PoolableComponent>();
            poolable.OnDespawn -= ResetItemData;
        }

        public bool CanSpawnItem()
        {
            return _activeItem.Count < _mapdata.maxItemSpawning;
        }
    }
}
