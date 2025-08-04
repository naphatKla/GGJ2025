using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Characters.CollectItemSystems.CollectableItems;
using Characters.Controllers;
using GameControl.SO;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

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
            obj.transform.SetParent(_state.ItemParent);
            obj.gameObject.SetActive(true);
        }
        
        public MapDataSO.ItemOption SpawnItem()
        {
            //Random
            var randomItem = RandomUtility.GetWeightedRandom(_storeItem);
            if (!_itemPools.TryGetValue(randomItem.id, out var pool)) return null;
            var obj = pool.Get();
            obj.transform.position = SpawnUtility.RandomInsideRegion(_itemRegionSize);
            
            return randomItem;
        }

        public void SpawnExpItem(int totalExp, Vector2 centerPosition)
        {
            var availableExpTypes = _storeItem
                .Where(i => i.itemObj is SoulItem)
                .Select(i => ((SoulItem)i.itemObj, i.id))
                .ToList();

            var expValues = availableExpTypes
                .Select(e => e.Item1.ItemData.Exp)
                .Distinct()
                .ToList();

            foreach (var exp in CalculateExpDrop(totalExp, expValues))
            {
                var match = availableExpTypes.Find(e => e.Item1.ItemData.Exp == exp);
                if (_itemPools.TryGetValue(match.id, out var pool))
                {
                    var obj = pool.Get();
                    
                    Vector2 randomOffset = Random.insideUnitCircle * Random.Range(1f, 2f);
                    obj.transform.position = centerPosition + randomOffset;
                }
            }
        }

        private List<int> CalculateExpDrop(int totalExp, List<int> expTypes)
        {
            var result = new List<int>();
            expTypes.Sort((a, b) => b.CompareTo(a));

            foreach (var exp in expTypes)
                while (totalExp >= exp)
                {
                    totalExp -= exp;
                    result.Add(exp);
                }

            if (totalExp > 0 && expTypes.Count > 0)
            {
                var smallest = expTypes[expTypes.Count - 1];
                result.Add(smallest);
            }

            return result;
        }

        public bool CanSpawnItem()
        {
            return _activeItem.Count < _mapdata.maxItemSpawning;
        }
        
        public void ClearAllItemsCompletely()
        {
            ReleaseAllItem();
            ClearAllItems();
        }

        
        public void ClearAllItems()
        {
            foreach (var pool in _itemPools.Values)
            {
                pool.Clear();
            }
            _activeItem.Clear();
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
