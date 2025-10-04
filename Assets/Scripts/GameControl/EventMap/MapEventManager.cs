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
    public interface IEventOverrideReceiver
    {
        void ApplyOverrides(IReadOnlyDictionary<string, object> overrides);
    }

    public sealed class EventOverrides
    {
        internal readonly Dictionary<string, object> Entry = new();
        internal readonly Dictionary<string, object> Instance = new();

        /// <summary>ตั้งค่าสำหรับทั้ง Entry และ Instance พร้อมกัน</summary>
        public EventOverrides Set(string key, object value)
        {
            Entry[key] = value;
            Instance[key] = value;
            return this;
        }

        /// <summary>ตั้งค่าเฉพาะ Entry (MapEventStorageEntry)</summary>
        public EventOverrides SetForEntry(string key, object value)
        {
            Entry[key] = value;
            return this;
        }

        /// <summary>ตั้งค่าเฉพาะ Instance (BaseMapEvent)</summary>
        public EventOverrides SetForInstance(string key, object value)
        {
            Instance[key] = value;
            return this;
        }
    }
    
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
        private static readonly Dictionary<Type, Dictionary<string, MemberInfo>> _memberCache = new();

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

        public async UniTask RunEventMapByID(string id, CancellationToken token, EventOverrides overrides)
        {
            if (!_mapStorageDict.TryGetValue(id, out var storage)) return;

            var playerPost = PlayerController.Instance.transform.position;
            var eventsToRun = GetFilteredEvents(storage);

            foreach (var entry in eventsToRun)
            {
                token.ThrowIfCancellationRequested();
                var workingEntry = CloneEntry(entry);

                // Apply overrides
                if (overrides != null && overrides.Entry.Count > 0)
                    ApplyOverridesToObject(workingEntry, overrides.Entry);

                PlayEntry(workingEntry, playerPost, overrides?.Instance);
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

        private void PlayEntry(MapEventStorageEntry entry, Vector3 playerPost, IReadOnlyDictionary<string, object> instanceOverrides)
        {
            var pool = GetOrCreatePool(entry.eventPrefab);
            var instance = pool.Get();

            instance.SetPool(pool);
            instance.transform.position = playerPost + entry.spawnPosition;
            instance.transform.rotation = Quaternion.Euler(entry.spawnEulerAngles);

            // override
            if (instanceOverrides != null && instanceOverrides.Count > 0)
            {
                if (instance is IEventOverrideReceiver recv)
                {
                    recv.ApplyOverrides(instanceOverrides);
                }
                else
                {
                    ApplyOverridesToObject(instance, instanceOverrides);
                }
            }

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

            _poolDict[prefab] = pool;
            return pool;
        }
        
        public void RunEvent(string id)
        {
            RunEventMapByID(id, _cts.Token, null).Forget();
        }
        
        public void RunEvent(string id, IReadOnlyDictionary<string, object> overrides)
        {
            var bag = new EventOverrides();
            if (overrides != null)
            {
                foreach (var kv in overrides)
                {
                    bag.Set(kv.Key, kv.Value);
                }
            }
            RunEventMapByID(id, _cts.Token, bag).Forget();
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
        
        [Title("▶️ Test Run (Odin Button)")]
        [InfoBox("ใส่ ID ที่ต้องการทดสอบ แล้วกดปุ่ม Run Test")]
        [SerializeField, LabelText("Event ID")] 
        private string _testId;
        
        [Button("Run Test"), GUIColor(0.3f, 0.8f, 0.3f)]
        private void RunTestById()
        {
            RunEvent(_testId);
        }
        
        #region Internal
        private static MapEventStorageEntry CloneEntry(MapEventStorageEntry original)
        {
            var t = original.GetType();
            if (!t.IsValueType)
            {
                var mi = t.GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
                return (MapEventStorageEntry)mi.Invoke(original, null);
            }
            return original;
        }

        private static void ApplyOverridesToObject(object target, IReadOnlyDictionary<string, object> data)
        {
            if (target == null || data == null || data.Count == 0) return;

            var type = target.GetType();
            if (!_memberCache.TryGetValue(type, out var members))
            {
                members = new Dictionary<string, MemberInfo>(StringComparer.OrdinalIgnoreCase);
                foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    members[f.Name] = f;
                foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    if (p.CanWrite) members[p.Name] = p;
                _memberCache[type] = members;
            }

            foreach (var kv in data)
            {
                if (!members.TryGetValue(kv.Key, out var m)) continue;

                try
                {
                    var val = ConvertIfNeeded(kv.Value, GetMemberType(m));
                    SetMemberValue(target, m, val);
                }
                catch
                {
                }
            }
        }

        private static Type GetMemberType(MemberInfo m)
        {
            return m switch
            {
                FieldInfo fi => fi.FieldType,
                PropertyInfo pi => pi.PropertyType,
                _ => typeof(object)
            };
        }

        private static void SetMemberValue(object target, MemberInfo m, object value)
        {
            switch (m)
            {
                case FieldInfo fi:
                    fi.SetValue(target, value);
                    break;
                case PropertyInfo pi:
                    pi.SetValue(target, value);
                    break;
            }
        }

        private static object ConvertIfNeeded(object value, Type targetType)
        {
            if (value == null) return null;

            var vType = value.GetType();
            if (targetType.IsAssignableFrom(vType)) return value;
            
            if (targetType.IsEnum && (value is string sEnum))
                return Enum.Parse(targetType, sEnum, ignoreCase: true);

            if (targetType == typeof(string)) return value.ToString();

            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                return value;
            }
        }
        #endregion
    }
}
