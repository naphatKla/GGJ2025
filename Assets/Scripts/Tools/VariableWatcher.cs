using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
using System.Reflection;
#endif

/// <summary>
/// แปะกับ GameObject เพื่อ "ดูค่าตัวแปรจากสคริปต์อื่น" ใน Inspector / Window
/// Editor-only (ส่วน Editor จะไม่ถูกใส่เข้า Build)
/// </summary>
public class VariableWatcher : MonoBehaviour
{
    [Serializable]
    public class WatchItem
    {
        [Tooltip("ชื่อที่อยากให้โชว์ใน Inspector / Window")]
        public string label;

        [Tooltip("ชื่อกลุ่ม (เอาไว้จัดกลุ่มใน Window)")]
        public string group;

        [Tooltip("Component/Script เป้าหมายที่อยากดูค่า")]
        public Component target;

        [Tooltip("ชื่อ field / property ที่เลือก (อย่าแก้เอง)")]
        public string memberKey;
    }

    [Tooltip("ลิสต์ตัวแปรที่อยากดูค่า")]
    public List<WatchItem> items = new List<WatchItem>();
}

#if UNITY_EDITOR

/// <summary>
/// Custom Inspector ของ VariableWatcher
/// </summary>
[CustomEditor(typeof(VariableWatcher))]
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

        // ปุ่ม Open Stats Window ด้านบน
        if (GUILayout.Button("Open Stats Window", GUILayout.Height(24)))
        {
            VariableWatcherWindow.ShowWindow((VariableWatcher)target);
        }

        EditorGUILayout.HelpBox(
            "ใช้ดูค่าตัวแปร/พร็อพเพอร์ตีจากสคริปต์อื่นใน Inspector หรือหน้าต่างแยก (Editor เท่านั้น)",
            MessageType.Info);

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
            int idx = _itemsProp.arraySize;
            _itemsProp.InsertArrayElementAtIndex(idx);
            var element = _itemsProp.GetArrayElementAtIndex(idx);
            element.FindPropertyRelative("label").stringValue = $"Watch {idx}";
            element.FindPropertyRelative("group").stringValue = string.Empty;
            element.FindPropertyRelative("target").objectReferenceValue = null;
            element.FindPropertyRelative("memberKey").stringValue = string.Empty;
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

        // ให้รีเฟรช Inspector ตอนเล่นเกมเพื่ออัปเดตค่า
        if (Application.isPlaying)
            Repaint();
    }

    private void DrawWatchItem(SerializedProperty element, int index)
    {
        var labelProp = element.FindPropertyRelative("label");
        var groupProp = element.FindPropertyRelative("group");
        var targetProp = element.FindPropertyRelative("target");
        var memberKeyProp = element.FindPropertyRelative("memberKey");

        EditorGUILayout.BeginVertical("box");

        // Header + ปุ่มลบ
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Item {index}", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("X", GUILayout.Width(20)))
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
            // ดึง field / property ด้วย reflection (รวม public + private)
            var type = targetComp.GetType();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var fields = type.GetFields(flags); // รวม private fields
            var props = type.GetProperties(flags)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0) // รวม private properties
                .ToArray();

            // รวมชื่อทั้งหมด
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

                // หา index ของตัวที่เลือกอยู่
                int currentIndex = Array.IndexOf(options, memberKeyProp.stringValue);

                int newIndex = EditorGUILayout.Popup("Member", currentIndex, options);
                if (newIndex >= 0 && newIndex < options.Length)
                {
                    memberKeyProp.stringValue = options[newIndex];
                }
            }

            // แสดง Value ปัจจุบัน (เฉพาะตอนเล่นเกม)
            if (Application.isPlaying && !string.IsNullOrEmpty(memberKeyProp.stringValue))
            {
                string displayName = string.IsNullOrEmpty(labelProp.stringValue)
                    ? memberKeyProp.stringValue
                    : labelProp.stringValue;

                object value = GetValue(targetComp, memberKeyProp.stringValue);
                EditorGUILayout.LabelField("Value", $"{displayName} = {value}");
            }
        }
        else
        {
            EditorGUILayout.HelpBox("ลาก Component/Script ที่อยากดูค่ามาใส่ในช่อง Target ก่อน", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// อ่านค่าจาก field / property ที่กำหนด โดยรองรับ private ด้วย
    /// memberKey รูปแบบ: "F: fieldName" หรือ "P: propName"
    /// </summary>
    internal static object GetValue(Component target, string memberKey)
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

/// <summary>
/// หน้าต่าง GUI แยก สำหรับดู Stats กลุ่มใหญ่ ๆ จาก VariableWatcher
/// </summary>
public class VariableWatcherWindow : EditorWindow
{
    private VariableWatcher _watcher;
    private Vector2 _scroll;

    public static void ShowWindow(VariableWatcher watcher)
    {
        var window = GetWindow<VariableWatcherWindow>("Variable Watcher");
        window._watcher = watcher;
        window.Show();
    }

    private void OnGUI()
    {
        // ให้เลือก watcher เปลี่ยนได้ใน window ด้วย
        _watcher = (VariableWatcher)EditorGUILayout.ObjectField(
            "Watcher", _watcher, typeof(VariableWatcher), true);

        if (!_watcher)
        {
            EditorGUILayout.HelpBox("Assign a VariableWatcher from the scene.", MessageType.Info);
            return;
        }

        var items = _watcher.items;
        if (items == null || items.Count == 0)
        {
            EditorGUILayout.HelpBox("No watch items on this VariableWatcher.", MessageType.Info);
            return;
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Press Play to see live updating values.", MessageType.Warning);
        }

        EditorGUILayout.Space();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        // Group โดยใช้ WatchItem.group
        var grouped = items
            .GroupBy(i => string.IsNullOrEmpty(i.group) ? "(Ungrouped)" : i.group)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var group in grouped)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(group.Key, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            foreach (var item in group)
            {
                EditorGUILayout.BeginHorizontal();

                string label = string.IsNullOrEmpty(item.label) ? item.memberKey : item.label;

                EditorGUILayout.LabelField(label, GUILayout.Width(180));

                string valueStr = "-";
                if (item.target != null && !string.IsNullOrEmpty(item.memberKey))
                {
                    object value = VariableWatcherEditor.GetValue(item.target, item.memberKey);
                    valueStr = value?.ToString() ?? "null";
                }
                else
                {
                    valueStr = "(no target / member)";
                }

                EditorGUILayout.LabelField(valueStr);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();

        if (Application.isPlaying)
            Repaint();
    }
}

#endif
