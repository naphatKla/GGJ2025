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

    public enum EventMode
    {
        PlaybySort,
        RandomAndPlay
    }

    public enum HitboxType
    {
        None,
        Box,
        Sphere,
        Capsule
    }
    
    public enum DataSetting
    {
        damage,
        deleteTime,
        delayPerform,
        delaybetweenEvent,
        chance
    }


    [Serializable]
    public class MapEventStorageEntry
    {
        [HideInInspector] public Transform spawnPointRef;
        
        [Title("➡️ Damage")]
        [FoldoutGroup("$GroupName")] public float damage = 5f;

        [Title("➡️ Time & Delay Data")]
        [FoldoutGroup("$GroupName")] public float deleteTime = 2f;
        [FoldoutGroup("$GroupName")] public float delayPerform = 1f;
        [PropertySpace] [FoldoutGroup("$GroupName")] [Title("➡️ Runtime Data (Serializable)")]
        public Vector3 spawnPosition;
        
        [FoldoutGroup("$GroupName")] public Vector3 spawnEulerAngles;
        [FoldoutGroup("$GroupName")] public HitboxType hitboxType = HitboxType.None;

        #region Box

        // Box
        [FoldoutGroup("$GroupName")] [ShowIf("hitboxType", HitboxType.Box)]
        public Vector3 boxSize;

        [FoldoutGroup("$GroupName")] [ShowIf("hitboxType", HitboxType.Box)]
        public Vector3 boxOffset;

        #endregion

        #region Sphere

        // Sphere
        [FoldoutGroup("$GroupName")] [ShowIf("hitboxType", HitboxType.Sphere)]
        public float sphereRadius;

        [FoldoutGroup("$GroupName")] [ShowIf("hitboxType", HitboxType.Sphere)]
        public Vector3 sphereOffset;

        #endregion

        #region Capsule

        // Capsule
        [FoldoutGroup("$GroupName")] [ShowIf("hitboxType", HitboxType.Capsule)]
        public float capsuleRadius;

        [FoldoutGroup("$GroupName")] [ShowIf("hitboxType", HitboxType.Capsule)]
        public float capsuleHeight;

        [FoldoutGroup("$GroupName")] [ShowIf("hitboxType", HitboxType.Capsule)]
        public Vector3 capsuleOffset;

        #endregion

        [PropertySpace] [FoldoutGroup("$GroupName")] [AssetSelector(Paths = "Assets/Prefabs/MapEvent")]
        public BaseMapEvent eventPrefab;

        [FoldoutGroup("$GroupName")] public float delayBetweenEvents = 0.3f;

        [FoldoutGroup("$GroupName")] [LabelText("Chance Event")]
        public bool enableChance;

        [FoldoutGroup("$GroupName")] [ShowIf("enableChance")] [Range(0, 1f)] [LabelText("Chance (0 - 1)")]
        public float chance = 0.5f;

        public string GroupName => eventPrefab != null ? eventPrefab.name : "Ungrouped";
    }

    [CreateAssetMenu(menuName = "EventMap/Container")]
    public class MapEventContainerSO : ScriptableObject
    {
        [Title("Event List")] public List<MapEventStorageEntry> entries = new();

        [Title("Min Max Config")] public bool enableMinMax;

        [ShowIf("enableMinMax")] public int minPlay;

        [ShowIf("enableMinMax")] public int maxPlay;

        [Title("Delay Config")] public DelayMode delayMode = DelayMode.Fixed;

        [ShowIf("@delayMode == DelayMode.Fixed")]
        public float defaultDelay = 0.3f;

        [ShowIf("@delayMode == DelayMode.Additive")]
        public float additiveStep = 0.1f;

        [Title("Default Prefab Asset")] [AssetSelector(Paths = "Assets/Prefabs/MapEvent")]
        public BaseMapEvent defaultPrefabAsset;

        [Title("Event Mod")] [HideIf("enableRandomMode")]
        public EventMode eventMode;

        public bool enableRandomMode;

        [Range(0f, 1f)] [ShowIf("enableRandomMode")]
        public float playBySortChance = 0.5f;

        [Range(0f, 1f)] [ShowIf("enableRandomMode")]
        public float randomAndPlayChance = 0.5f;


        [Title("Editor Tools")]
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

            var cumulativeDelay = delayMode == DelayMode.Additive ? GetLastDelayValue() : 0f;

            foreach (var go in Selection.gameObjects)
            {
                var prefabAsset = defaultPrefabAsset;

                var delay = delayMode switch
                {
                    DelayMode.Fixed => defaultDelay,
                    DelayMode.Additive => cumulativeDelay,
                    _ => 0f
                };
                var baseEvent = go.GetComponent<BaseMapEvent>();
                var entry = new MapEventStorageEntry
                {
                    spawnPosition = go.transform.position,
                    spawnEulerAngles = go.transform.eulerAngles,
                    eventPrefab = prefabAsset,
                    delayBetweenEvents = delay,
                    deleteTime = baseEvent.deletetime,
                    delayPerform = baseEvent.delayBeforePerform,
                    damage = baseEvent.damage
                };
                
                if (baseEvent is IBoxHitbox box)
                {
                    entry.hitboxType = HitboxType.Box;
                    entry.boxSize = box.Size;
                    entry.boxOffset = box.Offset;
                }
                else if (baseEvent is ISphereHitbox sphere)
                {
                    entry.hitboxType = HitboxType.Sphere;
                    entry.sphereRadius = sphere.Radius;
                    entry.sphereOffset = sphere.Offset;
                }
                else if (baseEvent is ICapsuleHitbox capsule)
                {
                    entry.hitboxType = HitboxType.Capsule;
                    entry.capsuleRadius = capsule.Radius;
                    entry.capsuleHeight = capsule.Height;
                    entry.capsuleOffset = capsule.Offset;
                }
                else
                {
                    entry.hitboxType = HitboxType.None;
                }

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
            var max = 0f;
            foreach (var entry in entries)
                max = Mathf.Max(max, entry.delayBetweenEvents);
            return max + additiveStep;
        }
#endif


#if UNITY_EDITOR
        [Title("Batch Hitbox Config Tool")] [InfoBox("ปรับ Hitbox ของทุก Entry หรือเฉพาะ Index ที่เลือกได้")]
        [InfoBox("ถ้าต้องการ Specifix Index สามารถพิมพ์ 0-5,7,9 จะได้ index 0,1,2,3,4,5,7,9 อัตโนมัติ")]
        public bool enableIndexFilter;

        [ShowIf("enableIndexFilter")] [LabelText("Index to Edit (comma-separated, e.g. 0,2,5)")]
        public string indexList = "";

        public HitboxType editType = HitboxType.None;

        [Title("Box Settings")] [LabelText("Box Size")] [ShowIf("@editType == HitboxType.Box")]
        public Vector3 boxSizeInput = new(2, 100, 0);

        [LabelText("Box Offset")] [ShowIf("@editType == HitboxType.Box")]
        public Vector3 boxOffsetInput = new(-1, 100, 0);

        [Title("Sphere Settings")] [LabelText("Sphere Radius")] [ShowIf("@editType == HitboxType.Sphere")]
        public float sphereRadiusInput = 1f;

        [LabelText("Sphere Offset")] [ShowIf("@editType == HitboxType.Sphere")]
        public Vector3 sphereOffsetInput = Vector3.zero;

        [Title("Capsule Settings")] [LabelText("Capsule Radius")] [ShowIf("@editType == HitboxType.Capsule")]
        public float capsuleRadiusInput = 0.5f;

        [LabelText("Capsule Height")] [ShowIf("@editType == HitboxType.Capsule")]
        public float capsuleHeightInput = 2f;
        [LabelText("Capsule Offset")] [ShowIf("@editType == HitboxType.Capsule")]
        public Vector3 capsuleOffsetInput = Vector3.zero;

        [Button("Apply Hitbox Settings", ButtonSizes.Medium)]
        private void ApplyHitboxSettings()
        {
            foreach (var i in GetTargetIndexes())
            {
                if (i < 0 || i >= entries.Count) continue;
                var e = entries[i];
                switch (e.hitboxType)
                {
                    case HitboxType.Box:
                        e.boxSize = boxSizeInput;
                        e.boxOffset = boxOffsetInput;
                        break;
                    case HitboxType.Sphere:
                        e.sphereRadius = sphereRadiusInput;
                        e.sphereOffset = sphereOffsetInput;
                        break;
                    case HitboxType.Capsule:
                        e.capsuleRadius = capsuleRadiusInput;
                        e.capsuleHeight = capsuleHeightInput;
                        e.capsuleOffset = capsuleOffsetInput;
                        break;
                }
            }

            MarkDirty();
        }

        /// <summary> คืน list ของ index ที่ต้องแก้ </summary>
        private List<int> GetTargetIndexes()
        {
            if (!enableIndexFilter || string.IsNullOrWhiteSpace(indexList))
            {
                var all = new List<int>();
                for (var i = 0; i < entries.Count; i++) all.Add(i);
                return all;
            }

            var indexes = new List<int>();
            var parts = indexList.Split(',');

            foreach (var p in parts)
            {
                var trimmed = p.Trim();
                if (trimmed.Contains("-"))
                {
                    // Handle range
                    var rangeParts = trimmed.Split('-');
                    if (rangeParts.Length == 2 &&
                        int.TryParse(rangeParts[0].Trim(), out var start) &&
                        int.TryParse(rangeParts[1].Trim(), out var end))
                    {
                        for (int i = start; i <= end; i++)
                            indexes.Add(i);
                    }
                }
                else
                {
                    // Single index
                    if (int.TryParse(trimmed, out var idx))
                        indexes.Add(idx);
                }
            }

            // Remove duplicates and out-of-range values
            var validIndexes = new List<int>();
            foreach (var i in indexes)
                if (i >= 0 && i < entries.Count && !validIndexes.Contains(i))
                    validIndexes.Add(i);

            return validIndexes;
        }
        
#if UNITY_EDITOR
        [Title("Batch Hitbox Type Tool")]
        [Button("Apply Hitbox Type", ButtonSizes.Medium)]
        private void ApplyHitboxType(HitboxType type)
        {
            foreach (var i in GetTargetIndexes())
            {
                if (i < 0 || i >= entries.Count) continue;
                entries[i].hitboxType = type;
            }
            
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
        
        [Title("Batch Set Data")]
        [Button("Apply Data", ButtonSizes.Medium)]
        private void ApplyHitboxType(DataSetting data, float num)
        {
            foreach (var i in GetTargetIndexes())
            {
                if (i < 0 || i >= entries.Count) continue;
                switch (data)
                {
                    case DataSetting.damage:
                        entries[i].damage = num;
                        break;
                    case DataSetting.deleteTime:
                        entries[i].deleteTime = num;
                        break;
                    case DataSetting.delayPerform:
                        entries[i].delayPerform = num;
                        break;
                    case DataSetting.delaybetweenEvent:
                        entries[i].delayBetweenEvents = num;
                        break;
                    case DataSetting.chance:
                        entries[i].chance = num;
                        break;
                }
            }
            
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
#endif


        private void MarkDirty()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
#endif
    }
}