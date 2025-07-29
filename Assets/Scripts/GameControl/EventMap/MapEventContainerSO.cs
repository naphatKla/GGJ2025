using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameControl.EventMap
{
    public enum DelayMode
    {
        Fixed,
        Additive
    }

    [Serializable]
    public class MapEventStorageEntry
    {
        [HideInInspector] public Transform spawnPointRef;

        [PropertySpace]
        [FoldoutGroup("$GroupName")]
        [Title("➡️ Runtime Data (Serializable)")]
        public Vector3 spawnPosition;

        [FoldoutGroup("$GroupName")]
        public Vector3 spawnEulerAngles;

        [PropertySpace]
        [FoldoutGroup("$GroupName")]
        [AssetSelector(Paths = "Assets/Prefabs/MapEvent")]
        public BaseMapEvent eventPrefab;

        [FoldoutGroup("$GroupName")]
        public float delayBetweenEvents = 0.3f;

        [HideInInspector]
        public string GroupName => eventPrefab != null ? eventPrefab.name : "Ungrouped";
    }

    [CreateAssetMenu(menuName = "EventMap/Container")]
    public class MapEventContainerSO : ScriptableObject
    {
        [Title("📦 Event List")]
        public List<MapEventStorageEntry> entries = new();

        [Title("⚙️ Delay Config")]
        public DelayMode delayMode = DelayMode.Fixed;

        [ShowIf("@delayMode == DelayMode.Fixed")]
        public float defaultDelay = 0.3f;

        [ShowIf("@delayMode == DelayMode.Additive")]
        public float additiveStep = 0.1f;
        
        [Title("📦 Default Prefab Asset")]
        [AssetSelector(Paths = "Assets/Prefabs/MapEvent")]
        public BaseMapEvent defaultPrefabAsset;

        [Title("🛠️ Editor Tools")]
        [Button("Capture From Selection (Clear)", ButtonSizes.Medium)]
        private void CaptureFromSelection()
        {
            entries.Clear();
            AddFromSelection_Internal();
        }

        [Button("Add From Selection (Keep Old)", ButtonSizes.Medium)]
        private void AddFromSelection()
        {
            AddFromSelection_Internal();
        }

        private void AddFromSelection_Internal()
        {
#if UNITY_EDITOR
            if (defaultPrefabAsset == null)
            {
                Debug.LogWarning("[MapEventContainerSO] Default prefab asset ยังไม่ถูกตั้งค่า!");
                return;
            }

            float cumulativeDelay = delayMode == DelayMode.Additive ? GetLastDelayValue() : 0f;

            foreach (var go in Selection.gameObjects)
            {
                var prefabAsset = defaultPrefabAsset;

                float delay = delayMode switch
                {
                    DelayMode.Fixed => defaultDelay,
                    DelayMode.Additive => cumulativeDelay,
                    _ => 0f
                };

                var entry = new MapEventStorageEntry
                {
                    spawnPosition = go.transform.position,
                    spawnEulerAngles = go.transform.eulerAngles,
                    eventPrefab = prefabAsset,
                    delayBetweenEvents = delay
                };

                entries.Add(entry);

                if (delayMode == DelayMode.Additive)
                    cumulativeDelay += additiveStep;
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Odin] Captured/Added {Selection.gameObjects.Length} entries with default prefab into {name}");
#endif
        }

#if UNITY_EDITOR
        private float GetLastDelayValue()
        {
            if (entries.Count == 0) return 0f;
            float max = 0f;
            foreach (var entry in entries)
                max = Mathf.Max(max, entry.delayBetweenEvents);
            return max + additiveStep;
        }
#endif
    }
}
