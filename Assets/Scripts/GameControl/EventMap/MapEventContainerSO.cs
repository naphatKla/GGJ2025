using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace GameControl.EventMap
{
    [Serializable]
    public class MapEventStorageEntry
    {
        [HideInInspector] public Transform spawnPointRef;

        [PropertySpace]
        [Title("➡️ Runtime Data (Serializable)")]
        public Vector3 spawnPosition;
        public Vector3 spawnEulerAngles;

        [PropertySpace]
        [AssetsOnly] [AssetSelector(Paths = "Assets/Prefabs/MapEvent")] public BaseMapEvent eventPrefab;
        public float delayBetweenEvents = 0.3f;
    }
    
    [CreateAssetMenu(menuName = "EventMap/Container")]
    public class MapEventContainerSO : ScriptableObject
    {
        public List<MapEventStorageEntry> entries = new List<MapEventStorageEntry>();
        
        [Title("⚙️ Editor Tools")]
        [Button("Capture From Selection", ButtonSizes.Large)]
        private void CaptureFromSelection()
        {
            entries.Clear();

            foreach (var go in Selection.gameObjects)
            {
                var evt = go.GetComponent<BaseMapEvent>();
                if (evt == null) continue; 

                var entry = new MapEventStorageEntry
                {
                    spawnPosition      = go.transform.position,
                    spawnEulerAngles   = go.transform.eulerAngles,
                    eventPrefab        = evt,
                    delayBetweenEvents = 0.3f,
                };
                entries.Add(entry);
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Odin] Captured {entries.Count} entries into {name}");
#endif
        }
    }
}