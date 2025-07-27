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

        private Dictionary<string, MapEventContainerSO> _mapStorageDict;
        private Dictionary<BaseMapEvent, ObjectPool<BaseMapEvent>> _poolDict = new();

        private void Awake()
        {
            _mapStorageDict = new Dictionary<string, MapEventContainerSO>();
            foreach (var entry in storageEntries)
                _mapStorageDict[entry.id] = entry.storage;
        }

        public async UniTaskVoid RunEventMapByID(string id)
        {
            if (!_mapStorageDict.TryGetValue(id, out var storage)) return;

            foreach (var entry in storage.entries)
            {
                var pool = GetOrCreatePool(entry.eventPrefab);
                var instance = pool.Get();

                instance.SetPool(pool);
                instance.transform.position = entry.spawnPosition;
                instance.transform.rotation = Quaternion.Euler(entry.spawnEulerAngles);

                await instance.Play();
                await UniTask.Delay(TimeSpan.FromSeconds(entry.delayBetweenEvents));
            }
        }

        private ObjectPool<BaseMapEvent> GetOrCreatePool(BaseMapEvent prefab)
        {
            if (_poolDict.TryGetValue(prefab, out var pool))
                return pool;

            pool = new ObjectPool<BaseMapEvent>(
                () =>
                {
                    var obj = Instantiate(prefab);
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
