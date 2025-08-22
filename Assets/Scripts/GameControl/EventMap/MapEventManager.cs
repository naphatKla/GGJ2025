using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Characters.Controllers;
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
        
        private CancellationTokenSource _cts;

        protected override void Awake()
        {
            base.Awake();
            _cts = new CancellationTokenSource();
            _mapStorageDict = new Dictionary<string, MapEventContainerSO>();
            foreach (var entry in storageEntries)
                _mapStorageDict[entry.id] = entry.storage;
        }
        
        private void OnDisable()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
        }

        public async UniTask RunEventMapByID(string id, CancellationToken token)
        {
            if (!_mapStorageDict.TryGetValue(id, out var storage)) return;

            var playerPost = PlayerController.Instance.transform.position;
            var eventsToRun = GetFilteredEvents(storage);
            foreach (var entry in eventsToRun)
            {
                token.ThrowIfCancellationRequested();
                PlayEntry(entry, playerPost);
                await UniTask.Delay(
                    TimeSpan.FromSeconds(GetDelayForEntry(entry, storage)), 
                    cancellationToken: token
                );
            }
        }

        private List<MapEventStorageEntry> GetFilteredEvents(MapEventContainerSO storage)
        {
            var events = new List<MapEventStorageEntry>();

            // 1) เลือก EventMode ตามโอกาส
            EventMode chosenMode = storage.enableRandomMode ? GetRandomEventMode(storage) : storage.eventMode;
            
            // 2) Filter ตาม Chance ของแต่ละ Event
            foreach (var entry in storage.entries)
            {
                bool shouldRun = !entry.enableChance || UnityEngine.Random.value <= entry.chance;
                if (shouldRun)
                    events.Add(entry);
            }

            // 3) Random
            if (chosenMode == EventMode.RandomAndPlay)
                events = ShuffleList(events);

            // 4) Min Max
            if (storage.enableMinMax && events.Count > 0)
            {
                int playCount = UnityEngine.Random.Range(storage.minPlay, storage.maxPlay + 1);
                playCount = Mathf.Clamp(playCount, 0, events.Count);

                if (events.Count > playCount)
                    events = events.GetRange(0, playCount);
            }
            return events;
        }

        private EventMode GetRandomEventMode(MapEventContainerSO storage)
        {
            float total = storage.playBySortChance + storage.randomAndPlayChance;
            if (total <= 0f) return EventMode.PlaybySort;

            float rand = UnityEngine.Random.value * total;
            if (rand <= storage.playBySortChance)
                return EventMode.PlaybySort;
            else
                return EventMode.RandomAndPlay;
        }

        private void PlayEntry(MapEventStorageEntry entry, Vector3 playerPost)
        {
            var pool = GetOrCreatePool(entry.eventPrefab);
            var instance = pool.Get();
            
            instance.SetPool(pool);
            instance.transform.position = playerPost + entry.spawnPosition;
            instance.transform.rotation = Quaternion.Euler(entry.spawnEulerAngles);

            instance.ApplyEffect(entry);
            instance.ApplyHitbox(entry);
            instance.Play().Forget();
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
        
        public void RunEvent(string id)
        {
            RunEventMapByID(id, _cts.Token).Forget();
        }
        
        [Title("▶️ Test Run (Odin Button)")]
        [InfoBox("ใส่ ID ที่ต้องการทดสอบ แล้วกดปุ่ม Run Test")]
        [SerializeField, LabelText("Event ID")] 
        private string _testId;
        
        [Button("Run Test"), GUIColor(0.3f, 0.8f, 0.3f)]
        private void RunTestById()
        {
            RunEvent(_testId);
        }
    }
}
