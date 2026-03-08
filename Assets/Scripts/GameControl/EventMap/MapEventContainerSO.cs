using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameControl.EventMap
{
    public enum DestinationDirection
    {
        Left,
        Right,
        Up,
        Down,
        UpLeft,
        UpRight,
        DownLeft,
        DownRight
    }
    
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
    public class DestinationNode
    {
        [HorizontalGroup("Row", Width = 110)]
        [LabelWidth(55)]
        public DestinationDirection direction;

        [HorizontalGroup("Row")]
        [LabelWidth(60)]
        [MinValue(0f)]
        public float distance = 1f;

        [HorizontalGroup("Row")]
        [LabelWidth(45)]
        [MinValue(0f)]
        public float speed = 5f;

        [HorizontalGroup("Row")]
        [LabelWidth(45)]
        [MinValue(0f)]
        public float delayAfterNode = 0f;
    }
    
    [Serializable]
    public class MapEventStorageEntry
    {
        [HideInInspector] public Transform spawnPointRef;
        
        [FoldoutGroup("$GroupName")] [BoxGroup("$GroupName/Data Setting")] public float damage = 5f;
        [FoldoutGroup("$GroupName")] [BoxGroup("$GroupName/Data Setting")] public float deleteTime = 2f;
        [FoldoutGroup("$GroupName")] [BoxGroup("$GroupName/Data Setting")] public float delayPerform = 1f;
        
        [FoldoutGroup("$GroupName")] [BoxGroup("$GroupName/Tranform")] public Vector3 spawnPosition;
        [FoldoutGroup("$GroupName")] [BoxGroup("$GroupName/Tranform")] public Vector3 spawnEulerAngles;
        
        [FoldoutGroup("$GroupName")] [BoxGroup("$GroupName/HitBox")] public HitboxType hitboxType = HitboxType.None;
        #region Box

        // Box
        [FoldoutGroup("$GroupName")] [BoxGroup("$GroupName/HitBox")] [ShowIf("hitboxType", HitboxType.Box)]
        public Vector3 boxSize;

        [FoldoutGroup("$GroupName")] [BoxGroup("$GroupName/HitBox")] [ShowIf("hitboxType", HitboxType.Box)]
        public Vector3 boxOffset;

        #endregion

        #region Sphere

        // Sphere
        [FoldoutGroup("$GroupName")] [BoxGroup("$GroupName/HitBox")] [ShowIf("hitboxType", HitboxType.Sphere)]
        public float sphereRadius;

        [FoldoutGroup("$GroupName")] [BoxGroup("$GroupName/HitBox")] [ShowIf("hitboxType", HitboxType.Sphere)]
        public Vector3 sphereOffset;

        #endregion

        #region Capsule

        // Capsule
        [FoldoutGroup("$GroupName")] [BoxGroup("$GroupName/HitBox")] [ShowIf("hitboxType", HitboxType.Capsule)]
        public float capsuleRadius;

        [FoldoutGroup("$GroupName")] [BoxGroup("$GroupName/HitBox")] [ShowIf("hitboxType", HitboxType.Capsule)]
        public float capsuleHeight;

        [FoldoutGroup("$GroupName")] [BoxGroup("$GroupName/HitBox")] [ShowIf("hitboxType", HitboxType.Capsule)]
        public Vector3 capsuleOffset;

        #endregion

        [FoldoutGroup("$GroupName")] [BoxGroup("$GroupName/Map Setting")][AssetSelector(Paths = "Assets/Prefabs/MapEvent")] 
        public BaseMapEvent eventPrefab;
        [FoldoutGroup("$GroupName")] [BoxGroup("$GroupName/Map Setting")] public float delayBetweenEvents = 0.3f;
        
        [FoldoutGroup("$GroupName")] [BoxGroup("$GroupName/Map Setting")] [LabelText("Chance Event")] public bool enableChance;
        [FoldoutGroup("$GroupName")] [BoxGroup("$GroupName/Map Setting")] [ShowIf("enableChance")] [Range(0, 1f)] [LabelText("Chance (0 - 1)")]
        public float chance = 0.5f;

        [FoldoutGroup("$GroupName")] [BoxGroup("$GroupName/Destination Setting")] [LabelText("Enable Destination")]
        public bool enableDestination;
        [FoldoutGroup("$GroupName")] [BoxGroup("$GroupName/Destination Setting")] [ShowIf("enableDestination")] [LabelText("Move Follow Destination")]
        public bool moveFollowDestination = true;

        [FoldoutGroup("$GroupName")] [BoxGroup("$GroupName/Destination Setting")] [ShowIf("enableDestination")]
        public List<DestinationNode> destinationList;


        public string GroupName => eventPrefab != null ? eventPrefab.name : "Ungrouped";

        public MapEventStorageEntry Clone()
        {
            var clone = (MapEventStorageEntry)MemberwiseClone();

            if (destinationList != null)
            {
                clone.destinationList = new List<DestinationNode>(destinationList.Count);
                foreach (var node in destinationList)
                    clone.destinationList.Add(new DestinationNode
                    {
                        direction = node.direction,
                        distance = node.distance,
                        speed = node.speed,
                        delayAfterNode = node.delayAfterNode
                    });
            }
            else
            {
                clone.destinationList = null;
            }

            return clone;
        }
    }

    [CreateAssetMenu(menuName = "EventMap/Container")]
    public class MapEventContainerSO : ScriptableObject
    {
        [Title("Event List")] public List<MapEventStorageEntry> entries = new();
        
        [FoldoutGroup("Import from scene Setting")]
        [EnumToggleButtons, LabelText("Delay Mode")]
        public DelayMode delayMode = DelayMode.Fixed;

        [FoldoutGroup("Import from scene Setting")]
        [ShowIf("@delayMode == DelayMode.Fixed")]
        [MinValue(0)] [LabelText("Default Delay")]
        public float defaultDelay = 0.3f;

        [FoldoutGroup("Import from scene Setting")]
        [ShowIf("@delayMode == DelayMode.Additive")]
        [MinValue(0)] [LabelText("Additive Step")]
        public float additiveStep = 0.1f;

        [FoldoutGroup("Import from scene Setting")]
        [Title("Default Prefab Asset")] [AssetSelector(Paths = "Assets/Prefabs/MapEvent")]
        public BaseMapEvent defaultPrefabAsset;
        
        [FoldoutGroup("Map Event Play Setting")]
        [ToggleLeft, LabelText("Enable Min/Max")]
        public bool enableMinMax;

        [FoldoutGroup("Map Event Play Setting")]
        [BoxGroup("Map Event Play Setting/Play Limit")]
        [ShowIf(nameof(enableMinMax))]
        [MinValue(0)] [LabelText("Min Play")]
        public int minPlay;

        [FoldoutGroup("Map Event Play Setting")]
        [BoxGroup("Map Event Play Setting/Play Limit")]
        [ShowIf(nameof(enableMinMax))]
        [MinValue(0)] [LabelText("Max Play")]
        [ValidateInput(nameof(ValidateMinMax), "Max Play ต้องมากกว่าหรือเท่ากับ Min Play")]
        public int maxPlay;
        private bool ValidateMinMax(int value) => !enableMinMax || value >= minPlay;

        [FoldoutGroup("Map Event Play Setting")]
        [Title("Event Mod")] [HideIf("enableRandomMode")]
        public EventMode eventMode;

        [FoldoutGroup("Map Event Play Setting")]
        public bool enableRandomMode;

        [FoldoutGroup("Map Event Play Setting")]
        [Range(0f, 1f)] [ShowIf("enableRandomMode")]
        public float playBySortChance = 0.5f;

        [FoldoutGroup("Map Event Play Setting")]
        [Range(0f, 1f)] [ShowIf("enableRandomMode")]
        public float randomAndPlayChance = 0.5f;

        [Title("Editor Tools")]
        [FoldoutGroup("Import from scene Setting")]
        [Button("ImportAll From Selection (Clear)", ButtonSizes.Large), GUIColor(0, 1, 0)]
        private void CaptureFromSelection()
        {
            entries.Clear();
            AddFromSelection_Internal();
        }

        [FoldoutGroup("Import from scene Setting")]
        [Button("Add From Selection (Keep Old)", ButtonSizes.Medium), GUIColor(1, 1, 0)]
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
                    delayPerform = baseEvent.previewDuration,
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
        [FoldoutGroup("แก้ไข Map Event")][InfoBox("กรอง Filter Data ของทุกอย่าง (Hitbox,Data,Prefab)")]
        [InfoBox("ถ้าต้องการ Specifix Index สามารถพิมพ์ 0-5,7,9 จะได้ index 0,1,2,3,4,5,7,9 อัตโนมัติ")]
        public bool enableIndexFilter;

        [FoldoutGroup("แก้ไข Map Event")][ShowIf("enableIndexFilter")] [LabelText("Index to Edit (comma-separated, e.g. 0,2,5)")]
        public string indexList = "";

        [FoldoutGroup("แก้ไข Map Event/ปรับ Hitbox")]
        [InfoBox("ส่วนนี้คือแก้ไขพวก Data ข้างในของ Hitbox เช่น Radius หรือ X Y ไม่ใช่การปรับ Type")]
        public HitboxType editType = HitboxType.None;

        [FoldoutGroup("แก้ไข Map Event/ปรับ Hitbox")]
        [Title("Box Settings")] [LabelText("Box Size")] [ShowIf("@editType == HitboxType.Box")]
        public Vector3 boxSizeInput = new(2, 100, 0);

        [FoldoutGroup("แก้ไข Map Event/ปรับ Hitbox")]
        [LabelText("Box Offset")] [ShowIf("@editType == HitboxType.Box")]
        public Vector3 boxOffsetInput = new(-1, 100, 0);

        [FoldoutGroup("แก้ไข Map Event/ปรับ Hitbox")]
        [Title("Sphere Settings")] [LabelText("Sphere Radius")] [ShowIf("@editType == HitboxType.Sphere")]
        public float sphereRadiusInput = 1f;

        [FoldoutGroup("แก้ไข Map Event/ปรับ Hitbox")]
        [LabelText("Sphere Offset")] [ShowIf("@editType == HitboxType.Sphere")]
        public Vector3 sphereOffsetInput = Vector3.zero;

        [FoldoutGroup("แก้ไข Map Event/ปรับ Hitbox")]
        [Title("Capsule Settings")] [LabelText("Capsule Radius")] [ShowIf("@editType == HitboxType.Capsule")]
        public float capsuleRadiusInput = 0.5f;

        [FoldoutGroup("แก้ไข Map Event/ปรับ Hitbox")]
        [LabelText("Capsule Height")] [ShowIf("@editType == HitboxType.Capsule")]
        public float capsuleHeightInput = 2f;
        
        [FoldoutGroup("แก้ไข Map Event/ปรับ Hitbox")]
        [LabelText("Capsule Offset")] [ShowIf("@editType == HitboxType.Capsule")]
        public Vector3 capsuleOffsetInput = Vector3.zero;

        [FoldoutGroup("แก้ไข Map Event/ปรับ Hitbox")]
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

        [FoldoutGroup("แก้ไข Map Event/ปรับ Map Event Prefab")]
        [InfoBox("ปรับ Prefab ของ Map Event ตามของใหม่ด้านล่างนี้")]
        [Title("New Prefab Asset")] [AssetSelector(Paths = "Assets/Prefabs/MapEvent")]
        public BaseMapEvent newPrefabAsset;
        
        [FoldoutGroup("แก้ไข Map Event/ปรับ Map Event Prefab")]
        [Button("Set Default Prefab", ButtonSizes.Medium)]
        private void ApplyDefaultPrefab()
        {
            if (newPrefabAsset == null)
            {
                Debug.LogWarning("[MapEventContainerSO] Default prefab asset ยังไม่ถูกตั้งค่า!");
                return;
            }

            foreach (var i in GetTargetIndexes())
            {
                if (i < 0 || i >= entries.Count) continue;
                entries[i].eventPrefab = newPrefabAsset;
            }

            MarkDirty();
            Debug.Log($"[Odin] Set Default Prefab ({newPrefabAsset.name}) ให้กับ {GetTargetIndexes().Count} entries");
        }

#if UNITY_EDITOR
        [FoldoutGroup("แก้ไข Map Event/ปรับ Hitbox")]
        [InfoBox("ส่วนนี้เป็นการปรับ Hitbox Type ของ MapEvent ใน List")]
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
        
        [FoldoutGroup("แก้ไข Map Event/ปรับ MapEvent Data (damage,delayPerform)")]
        [Button("Apply Data", ButtonSizes.Medium)]
        private void ApplyDataConfig(DataSetting data, float num)
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