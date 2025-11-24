using System;
using System.Collections.Generic;
using System.Reflection;
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
    public class MapCatagory
    {
        [FoldoutGroup("$catagoryName")]
        public string catagoryName;
        [FoldoutGroup("$catagoryName")]
        public List<StorageEntry> storageEntries;
    }
    
    [Serializable]
    public struct StorageEntry
    {
        public string id;
        public MapEventContainerSO storage;
    }
    
    public class MapEventManager : MMSingleton<MapEventManager>
    {
        [SerializeField] private List<MapCatagory> catagorieEntries;

        [SerializeField] private Transform eventMapParent;

        private Dictionary<string, MapEventContainerSO> _mapStorageDict;
        private Dictionary<BaseMapEvent, ObjectPool<BaseMapEvent>> _poolDict = new();
        
        private CancellationTokenSource _cts;
        private static readonly Dictionary<Type, Dictionary<string, MemberInfo>> _memberCache = new();

        protected override void Awake()
        {
            base.Awake();
            _cts = new CancellationTokenSource();
            _mapStorageDict = new Dictionary<string, MapEventContainerSO>();
            foreach (var catagory in catagorieEntries) 
            foreach (var entry in catagory.storageEntries)
                _mapStorageDict[entry.id] = entry.storage;
        }
        
        private void OnDisable()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
        }

        public async UniTask RunEventMapByID(string id, CancellationToken token, EventOverrides overrides)
        {
            if (!_mapStorageDict.TryGetValue(id, out var storage)) return;

            var playerPost   = PlayerController.Instance.transform.position;
            var eventsToRun  = GetFilteredEvents(storage);

            foreach (var entry in eventsToRun)
            {
                token.ThrowIfCancellationRequested();
                var workingEntry = entry.Clone();
                PlayEntry(workingEntry, playerPost, overrides);

                await UniTask.Delay(
                    TimeSpan.FromSeconds(GetDelayForEntry(workingEntry, storage)),
                    cancellationToken: token
                );
            }
        }


        private List<MapEventStorageEntry> GetFilteredEvents(MapEventContainerSO storage)
        {
            var events = new List<MapEventStorageEntry>();

            // 1) เลือก EventMode ตามโอกาส
            EventMode chosenMode = storage.enableRandomMode ? GetRandomEventMode(storage) : storage.eventMode;
            
            // 2) Filter ตาม Chance ของแต่ละ Event + Modify
            foreach (var entry in storage.entries)
            {
                bool shouldRun = !entry.enableChance || UnityEngine.Random.value <= entry.chance;
                if (shouldRun) events.Add(entry);
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
            return (rand <= storage.playBySortChance) ? EventMode.PlaybySort : EventMode.RandomAndPlay;
        }

        private void PlayEntry(MapEventStorageEntry entry, Vector3 playerPost, EventOverrides overrides)
        {
            var pool = GetOrCreatePool(entry.eventPrefab);
            var instance = pool.Get();

            instance.SetPool(pool);
            instance.transform.position = playerPost + entry.spawnPosition;
            instance.transform.rotation = Quaternion.Euler(entry.spawnEulerAngles);

            // override
            overrides?.ApplyTo(entry, instance);

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
                    return obj;
                },
                obj =>
                {
                    if (obj == null) return;
                    //obj.ClearVFX();
                    obj.SetPool(pool);
                    obj.gameObject.SetActive(true);
                },
                obj =>
                {
                    if (obj == null || obj.gameObject == null) return;
                    //obj.ClearVFX();
                    obj.gameObject.SetActive(false);
                },
                obj =>
                {
                    if (obj == null) return;
                    obj.ClearVFX();
                    Destroy(obj.gameObject);
                },
                false, 10, 100
            );

            for (int i = 0; i < prefab.prewarmCount; i++)
            {
                var obj = Instantiate(prefab, eventMapParent);
                pool.Release(obj);
            }

            _poolDict[prefab] = pool;
            return pool;
        }
        
        public void RunEvent(string id, Action<EventOverrides> configure)
        {
            EventOverrides bag = null;
            if (configure != null)
            {
                bag = new EventOverrides();
                configure(bag);
            }
            RunEventMapByID(id, _cts.Token, bag).Forget();
        }
        
        [Title("▶️ Test Run (Odin Button)")][FoldoutGroup("Test Map Event")]
        [InfoBox("ใส่ ID ที่ต้องการทดสอบ แล้วกดปุ่ม Run Test")]
        [SerializeField, LabelText("Event ID")] 
        private string _testId;
     
        [SerializeField, LabelText("Override Damage")][FoldoutGroup("Test Map Event")]
        private float _testdmg;
        
        [Button("Run Test"), GUIColor(0.3f, 0.8f, 0.3f)]
        private void RunTestById()
        {
            RunEvent(_testId, o => o.ForEntry(e =>
            {
                e.damage = _testdmg;
            }));
        }
        
        #region Event Override
        public sealed class EventOverrides
        {
            private readonly List<Action<MapEventStorageEntry>> _entrySetters = new();
            private readonly List<Action<BaseMapEvent>> _instanceSetters = new();

            public EventOverrides ForEntry(Action<MapEventStorageEntry> set)
            {
                if (set != null) _entrySetters.Add(set);
                return this;
            }

            public EventOverrides ForInstance<TEvent>(Action<TEvent> set)
                where TEvent : BaseMapEvent
            {
                if (set != null)
                {
                    _instanceSetters.Add(be =>
                    {
                        if (be is TEvent t) set(t);
                    });
                }
                return this;
            }

            internal void ApplyTo(MapEventStorageEntry entry, BaseMapEvent instance)
            {
                for (int i = 0; i < _entrySetters.Count; i++) _entrySetters[i]?.Invoke(entry);
                for (int i = 0; i < _instanceSetters.Count; i++) _instanceSetters[i]?.Invoke(instance);
            }
        }
        #endregion
    }
}
