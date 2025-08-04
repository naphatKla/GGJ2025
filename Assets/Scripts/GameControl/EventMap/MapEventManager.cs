using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Pool;

namespace GameControl.EventMap
{
    [Serializable]
    public struct StorageEntry
    {
        public string id;
        public MapEventContainerSO storage;
    }
    
    public class MapEventManager : MMSingleton<MapEventManager>
    {
        [SerializeField] private List<StorageEntry> storageEntries;

        [SerializeField] private Transform eventMapParent;

        private Dictionary<string, MapEventContainerSO> _mapStorageDict;
        private Dictionary<BaseMapEvent, ObjectPool<BaseMapEvent>> _poolDict = new();

        protected override void Awake()
        {
            base.Awake();
            _mapStorageDict = new Dictionary<string, MapEventContainerSO>();
            foreach (var entry in storageEntries)
                _mapStorageDict[entry.id] = entry.storage;
        }

        public async UniTaskVoid RunEventMapByID(string id)
        {
            if (!_mapStorageDict.TryGetValue(id, out var storage)) return;

            var eventsToRun = GetFilteredEvents(storage);
            foreach (var entry in eventsToRun)
            {
                await PlayEntry(entry);
                await UniTask.Delay(TimeSpan.FromSeconds(GetDelayForEntry(entry, storage)));
            }
        }

        private List<MapEventStorageEntry> GetFilteredEvents(MapEventContainerSO storage)
        {
            var events = new List<MapEventStorageEntry>();

            foreach (var entry in storage.entries)
            {
                bool shouldRun = !entry.enableChance || UnityEngine.Random.value <= entry.chance;
                if (shouldRun)
                    events.Add(entry);
            }

            return storage.randomAllEvent ? ShuffleList(events) : events;
        }

        private async UniTask PlayEntry(MapEventStorageEntry entry)
        {
            var pool = GetOrCreatePool(entry.eventPrefab);
            var instance = pool.Get();

            instance.SetPool(pool);
            instance.transform.position = entry.spawnPosition;
            instance.transform.rotation = Quaternion.Euler(entry.spawnEulerAngles);

            await instance.Play();
        }

        private float GetDelayForEntry(MapEventStorageEntry entry, MapEventContainerSO storage)
        {
            return storage.delayMode switch
            {
                DelayMode.Fixed => storage.defaultDelay,
                DelayMode.Additive => entry.delayBetweenEvents,
                _ => 0.3f
            };
        }
        
        private List<T> ShuffleList<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int rand = UnityEngine.Random.Range(i, list.Count);
                (list[i], list[rand]) = (list[rand], list[i]);
            }
            return list;
        }

        private ObjectPool<BaseMapEvent> GetOrCreatePool(BaseMapEvent prefab)
        {
            if (_poolDict.TryGetValue(prefab, out var pool)) return pool;
            pool = new ObjectPool<BaseMapEvent>(
                () =>
                {
                    var obj = Instantiate(prefab, eventMapParent);
                    obj.SetPool(pool);
                    return obj;
                },
                obj => obj.gameObject.SetActive(true),
                obj => obj.gameObject.SetActive(false),
                obj => Destroy(obj.gameObject),
                false, 10, 100
            );

            _poolDict[prefab] = pool;
            return pool;
        }
        
        [Title("▶️ Test Run (Odin Button)")]
        [InfoBox("ใส่ ID ที่ต้องการทดสอบ แล้วกดปุ่ม Run Test")]
        [SerializeField, LabelText("Event ID")] 
        private string _testId;
        
        [Button("Run Test"), GUIColor(0.3f, 0.8f, 0.3f)]
        private void RunTestById()
        {
            RunEventMapByID(_testId).Forget();
        }
    }
}
