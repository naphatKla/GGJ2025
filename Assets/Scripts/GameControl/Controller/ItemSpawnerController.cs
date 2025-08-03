using System.Collections;
using System.Collections.Generic;
using Characters.CollectItemSystems.CollectableItems;
using Characters.Controllers;
using GameControl.SO;
using UnityEngine;
using UnityEngine.Pool;

namespace GameControl.Controller
{
    public class ItemSpawnerController
    {
        private MapDataSO _mapdata;
        private SpawnerStateController _state;
        private readonly Vector2 _itemRegionSize;
        private List<MapDataSO.ItemOption> _storeItem;
        private List<BaseCollectableItem> _activeItem;
        
        private Dictionary<string, ObjectPool<BaseCollectableItem>> _itemPools;

        public ItemSpawnerController(SO.MapDataSO mapData, SpawnerStateController state, Vector2 spawnRegion)
        {
            _mapdata = mapData;
            _state = state;
            _itemRegionSize = spawnRegion;
        }
        
        public void PrewarmItem()
        {
            _storeItem = new List<MapDataSO.ItemOption>();
            _activeItem = new List<BaseCollectableItem>();
            _itemPools = new Dictionary<string, ObjectPool<BaseCollectableItem>>();
            
            foreach (var data in _mapdata.ItemOptions)
            {
                var cloned = new MapDataSO.ItemOption()
                {
                    id = data.id,
                    itemObj = data.itemObj,
                    prewarmCount = data.prewarmCount,
                    useCustomInterval = data.useCustomInterval,
                    customInterval = data.customInterval
                };

                _storeItem.Add(cloned);
              
                _itemPools[cloned.id] = new ObjectPool<BaseCollectableItem>(
                    () => CreateFunc(cloned),
                    obj => ActionOnGet(obj, cloned),
                    obj => ActionOnRelease(obj, cloned),
                    obj => ActionOnDestroy(obj, cloned),
                    false
                );
            }
        }
        
        private BaseCollectableItem CreateFunc(MapDataSO.ItemOption option)
        {
            var obj = Object.Instantiate(option.itemObj);
            var baseCollectable = obj.GetComponent<BaseCollectableItem>();
            baseCollectable.OnThisItemCollected = () => _itemPools[option.id].Release(baseCollectable);
            return baseCollectable;
        }
        
        public void ActionOnDestroy(BaseCollectableItem obj, MapDataSO.ItemOption option)
        {
            Object.Destroy(obj.gameObject);
        }

        public void ActionOnRelease(BaseCollectableItem obj, MapDataSO.ItemOption option)
        {
            obj.gameObject.SetActive(false);
            _activeItem.Remove(obj);
        }
        
        private void ActionOnGet(BaseCollectableItem obj, MapDataSO.ItemOption option)
        {
            _activeItem.Add(obj);
            obj.transform.position = SpawnUtility.RandomInsideRegion(_itemRegionSize);
            obj.transform.SetParent(_state.ItemParent);
            obj.gameObject.SetActive(true);
        }
        
        public MapDataSO.ItemOption SpawnItem()
        {
            //Random
            var randomItem = RandomUtility.GetWeightedRandom(_storeItem);
            if (!_itemPools.TryGetValue(randomItem.id, out var pool)) return null;
            pool.Get();
            
            return randomItem;
        }

        public bool CanSpawnItem()
        {
            return _activeItem.Count < _mapdata.maxItemSpawning;
        }
        
        public void ReleaseAllItem()
        {
            foreach (var item in _activeItem.ToArray())
            {
                item.OnThisItemCollected?.Invoke();
            }
        }
    }
}
