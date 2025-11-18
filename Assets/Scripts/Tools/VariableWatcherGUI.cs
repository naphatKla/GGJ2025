using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// แปะกับ GameObject เพื่อ "ดูค่าตัวแปรจากสคริปต์อื่น"
/// - ตั้งค่า list ตัวแปรที่จะดูได้จาก Inspector
/// - ระหว่างเล่นเกม กดปุ่ม toggleKey เพื่อเปิด/ปิดหน้าต่าง Overlay (OnGUI)
/// </summary>
public class VariableWatcherGUI : MonoBehaviour
{
    [Serializable]
    public class WatchItem
    {
        [Tooltip("ชื่อที่อยากให้โชว์ใน Inspector / Overlay")]
        public string label;

        [Tooltip("ชื่อกลุ่ม (ใช้จัดกลุ่มใน Overlay)")]
        public string group;

        [Tooltip("Component/Script เป้าหมายที่อยากดูค่า")]
        public Component target;

        [Tooltip("ชื่อ field / property ที่เลือก (อย่าแก้เอง)")]
        public string memberKey; // รูปแบบ "F: fieldName" หรือ "P: propName"
    }

    public enum AnchorMode
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    [Header("Overlay")]
    [Tooltip("ปุ่มสำหรับเปิด/ปิดหน้าต่างดูค่า")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F2;

    [SerializeField] private bool _debugVisible = false;

    [Tooltip("ตำแหน่ง anchor เริ่มต้นของหน้าต่าง")]
    [SerializeField] private AnchorMode anchor = AnchorMode.BottomLeft;

    [Tooltip("Rect: x,y = offset จาก anchor | w,h = ขนาดหน้าต่าง")]
    [SerializeField] private Rect _windowRect = new Rect(20, 20, 420, 400);

    private bool _windowInitialized = false;

    [Tooltip("ลิสต์ตัวแปรที่อยากดูค่า")]
    public List<WatchItem> items = new List<WatchItem>();

    private Vector2 _scroll;
    private GUIStyle _hdrStyle;
    private GUIStyle _groupStyle;
    private GUIStyle _kvStyle;

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            _debugVisible = !_debugVisible;
    }

    private void InitStylesIfNeeded()
    {
        if (_hdrStyle == null)
            _hdrStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14
            };

        if (_groupStyle == null)
            _groupStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 13
            };

        if (_kvStyle == null)
            _kvStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true
            };
    }

    private void OnGUI()
    {
        if (!_debugVisible) return;

        InitStylesIfNeeded();

        // เซ็ตตำแหน่งเริ่มต้นตาม anchor + offset "ครั้งเดียว"
        if (!_windowInitialized)
        {
            ApplyAnchor();
            _windowInitialized = true;
        }

        _windowRect = GUI.Window(0xA11CE6, _windowRect, DrawOverlayWindow,
            $"Variable Watcher ({toggleKey} to toggle)");
    }

    private void ApplyAnchor()
    {
        float width = _windowRect.width;
        float height = _windowRect.height;

        // x,y ของ Rect ใช้เป็น offset
        float offsetX = _windowRect.x;
        float offsetY = _windowRect.y;

        float x = 0f;
        float y = 0f;

        switch (anchor)
        {
            case AnchorMode.TopLeft:
                x = offsetX;
                y = offsetY;
                break;

            case AnchorMode.TopCenter:
                x = (Screen.width - width) * 0.5f + offsetX;
                y = offsetY;
                break;

            case AnchorMode.TopRight:
                x = Screen.width - width - offsetX;
                y = offsetY;
                break;

            case AnchorMode.MiddleLeft:
                x = offsetX;
                y = (Screen.height - height) * 0.5f + offsetY;
                break;

            case AnchorMode.MiddleCenter:
                x = (Screen.width - width) * 0.5f + offsetX;
                y = (Screen.height - height) * 0.5f + offsetY;
                break;

            case AnchorMode.MiddleRight:
                x = Screen.width - width - offsetX;
                y = (Screen.height - height) * 0.5f + offsetY;
                break;

            case AnchorMode.BottomLeft:
                x = offsetX;
                y = Screen.height - height - offsetY;
                break;

            case AnchorMode.BottomCenter:
                x = (Screen.width - width) * 0.5f + offsetX;
                y = Screen.height - height - offsetY;
                break;

            case AnchorMode.BottomRight:
                x = Screen.width - width - offsetX;
                y = Screen.height - height - offsetY;
                break;
        }

        _windowRect = new Rect(x, y, width, height);
    }

    private void DrawOverlayWindow(int id)
    {
        GUILayout.BeginVertical();
        _scroll = GUILayout.BeginScrollView(_scroll);

        if (items == null || items.Count == 0)
        {
            GUILayout.Label("No watch items set.", _kvStyle);
        }
        else
        {
            // Group โดยใช้ WatchItem.group
            var grouped = items
                .GroupBy(i => string.IsNullOrEmpty(i.group) ? "(Ungrouped)" : i.group)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var group in grouped)
            {
                GUILayout.Space(4);
                GUILayout.Label(group.Key, _groupStyle);
                GUILayout.BeginVertical("box");

                foreach (var item in group)
                {
                    string label = string.IsNullOrEmpty(item.label) ? item.memberKey : item.label;

                    string valueStr;
                    if (item.target != null && !string.IsNullOrEmpty(item.memberKey))
                    {
                        object value = GetValueRuntime(item.target, item.memberKey);
                        valueStr = value?.ToString() ?? "null";
                    }
                    else
                    {
                        valueStr = "(no target / member)";
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(label + ":", GUILayout.Width(180));
                    GUILayout.Label(valueStr, _kvStyle);
                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();
            }
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        // ลากหัวหน้าต่างได้
        GUI.DragWindow(new Rect(0, 0, 10000, 18));
    }

    /// <summary>
    /// อ่านค่าจาก field / property ที่กำหนด โดยรองรับ private ด้วย (runtime)
    /// memberKey รูปแบบ: "F: fieldName" หรือ "P: propName"
    /// </summary>
    private static object GetValueRuntime(Component target, string memberKey)
    {
        if (string.IsNullOrEmpty(memberKey) || target == null) return "—";

        var type = target.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        if (memberKey.StartsWith("F: "))
        {
            string name = memberKey.Substring(3);
            var fi = type.GetField(name, flags);
            if (fi != null) return fi.GetValue(target) ?? "null";
        }
        else if (memberKey.StartsWith("P: "))
        {
            string name = memberKey.Substring(3);
            var pi = type.GetProperty(name, flags);
            if (pi != null && pi.CanRead && pi.GetIndexParameters().Length == 0)
                return pi.GetValue(target) ?? "null";
        }

        return "N/A";
    }
}

#if UNITY_EDITOR

/// <summary>
/// Custom Inspector ของ VariableWatcherGUI:
/// - เลือก target / member ได้
/// - มีปุ่ม + แทรก item ใต้ตัวปัจจุบัน
/// - มีปุ่ม ▲ / ▼ ย้ายลำดับ
/// </summary>
[CustomEditor(typeof(VariableWatcherGUI))]
public class VariableWatcherEditor : Editor
{
    private SerializedProperty _itemsProp;

    private void OnEnable()
    {
        _itemsProp = serializedObject.FindProperty("items");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Variable Watcher", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "แปะกับ GameObject แล้วกำหนดรายการตัวแปรที่อยากดูค่า.\n" +
            "ระหว่างเล่นเกม กดปุ่ม toggleKey เพื่อเปิด/ปิด Overlay (OnGUI).\n" +
            "Anchor + Rect.x,y จะใช้เป็น offset, Rect.w,h คือขนาดหน้าต่าง.",
            MessageType.Info);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("toggleKey"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("anchor"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_windowRect"),
            new GUIContent("Offset (x,y) & Size (w,h)"));

        EditorGUILayout.Space();

        // วาดแต่ละ WatchItem
        for (int i = 0; i < _itemsProp.arraySize; i++)
        {
            var element = _itemsProp.GetArrayElementAtIndex(i);
            DrawWatchItem(element, i);
            EditorGUILayout.Space(4);
        }

        EditorGUILayout.Space();

        // แถวล่าง: Add / Delete All
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Variable (+)", GUILayout.Height(22)))
        {
            InsertNewItem(_itemsProp.arraySize);
        }

        if (_itemsProp.arraySize > 0)
        {
            if (GUILayout.Button("Delete All", GUILayout.Width(90), GUILayout.Height(22)))
            {
                if (EditorUtility.DisplayDialog("Delete All?",
                        "Do you want to delete all watch entries?", "OK", "Cancel"))
                {
                    _itemsProp.ClearArray();
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawWatchItem(SerializedProperty element, int index)
    {
        var labelProp = element.FindPropertyRelative("label");
        var groupProp = element.FindPropertyRelative("group");
        var targetProp = element.FindPropertyRelative("target");
        var memberKeyProp = element.FindPropertyRelative("memberKey");

        EditorGUILayout.BeginVertical("box");

        // Header + ปุ่ม reorder / แทรก / ลบ
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Item {index}", EditorStyles.boldLabel);

        if (GUILayout.Button("▲", GUILayout.Width(22)))
        {
            if (index > 0)
            {
                _itemsProp.MoveArrayElement(index, index - 1);
            }
        }
        if (GUILayout.Button("▼", GUILayout.Width(22)))
        {
            if (index < _itemsProp.arraySize - 1)
            {
                _itemsProp.MoveArrayElement(index, index + 1);
            }
        }

        if (GUILayout.Button("+", GUILayout.Width(22)))
        {
            InsertNewItem(index + 1);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }

        if (GUILayout.Button("X", GUILayout.Width(22)))
        {
            _itemsProp.DeleteArrayElementAtIndex(index);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.PropertyField(labelProp, new GUIContent("Label"));
        EditorGUILayout.PropertyField(groupProp, new GUIContent("Group"));
        EditorGUILayout.PropertyField(targetProp, new GUIContent("Target Component"));

        var targetComp = targetProp.objectReferenceValue as Component;

        if (targetComp != null)
        {
            // ดึง fields และ properties (public + private, non-indexer)
            var type = targetComp.GetType();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var fields = type.GetFields(flags);
            var props = type.GetProperties(flags)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .ToArray();

            var allOptions = new List<string>();
            foreach (var f in fields)
                allOptions.Add($"F: {f.Name}");
            foreach (var p in props)
                allOptions.Add($"P: {p.Name}");

            if (allOptions.Count == 0)
            {
                EditorGUILayout.HelpBox("No fields or properties found on this component.", MessageType.Info);
            }
            else
            {
                string[] options = allOptions.ToArray();
                int currentIndex = Array.IndexOf(options, memberKeyProp.stringValue);

                int newIndex = EditorGUILayout.Popup("Member", currentIndex, options);
                if (newIndex >= 0 && newIndex < options.Length)
                {
                    memberKeyProp.stringValue = options[newIndex];
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("ลาก Component/Script ที่อยากดูค่ามาใส่ในช่อง Target ก่อน", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// แทรก item ใหม่ที่ index ที่กำหนด แล้วตั้งค่าพื้นฐานให้
    /// </summary>
    private void InsertNewItem(int index)
    {
        if (index < 0 || index > _itemsProp.arraySize) index = _itemsProp.arraySize;

        _itemsProp.InsertArrayElementAtIndex(index);
        var element = _itemsProp.GetArrayElementAtIndex(index);
        element.FindPropertyRelative("label").stringValue = $"Watch {index}";
        element.FindPropertyRelative("group").stringValue = string.Empty;
        element.FindPropertyRelative("target").objectReferenceValue = null;
        element.FindPropertyRelative("memberKey").stringValue = string.Empty;
    }
}

#endif
