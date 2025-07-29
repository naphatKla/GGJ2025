#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameControl.EventMap
{
    public class MapEventSceneEditor : EditorWindow
    {
        private enum Mode
        {
            Fix,
            Additive,
            Duplicate
        }

        private Mode transformMode = Mode.Fix;

        private Vector3 positionEdit = Vector3.zero;
        private Vector3 rotationEdit = Vector3.zero;
        private Vector3 scaleEdit = Vector3.one;

        // Duplicate options
        private int duplicateCount = 5;
        private string nameSuffix = "_Clone";

        [MenuItem("Tools/MapEvent Scene Editor")]
        public static void ShowWindow()
        {
            GetWindow<MapEventSceneEditor>("MapEvent Scene Editor");
        }

        private void OnGUI()
        {
            GUILayout.Label("Transform Edit", EditorStyles.boldLabel);

            transformMode = (Mode)EditorGUILayout.EnumPopup("Edit Mode", transformMode);

            EditorGUILayout.Space();
            positionEdit = EditorGUILayout.Vector3Field("Position", positionEdit);
            rotationEdit = EditorGUILayout.Vector3Field("Rotation", rotationEdit);
            scaleEdit = EditorGUILayout.Vector3Field("Scale", scaleEdit);

            if (transformMode == Mode.Duplicate)
            {
                EditorGUILayout.Space();
                duplicateCount = EditorGUILayout.IntField("Duplicate Count", Mathf.Max(1, duplicateCount));
                nameSuffix = EditorGUILayout.TextField("Name Suffix", nameSuffix);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Apply To Selected"))
            {
                ApplyToSelection();
            }
        }

        private void ApplyToSelection()
        {
            var selectedObjects = Selection.transforms;

            if (selectedObjects.Length == 0)
            {
                Debug.LogWarning("No objects selected.");
                return;
            }

            Undo.RegisterCompleteObjectUndo(this, "MapEvent Transform Edit");

            if (transformMode == Mode.Fix || transformMode == Mode.Additive)
            {
                foreach (var t in selectedObjects)
                {
                    Undo.RecordObject(t, "Transform Edit");

                    if (transformMode == Mode.Fix)
                    {
                        t.position = positionEdit;
                        t.eulerAngles = rotationEdit;
                        t.localScale = scaleEdit;
                    }
                    else if (transformMode == Mode.Additive)
                    {
                        t.position += positionEdit;
                        t.eulerAngles += rotationEdit;
                        t.localScale += scaleEdit;
                    }
                }
            }
            else if (transformMode == Mode.Duplicate)
            {
                foreach (var t in selectedObjects)
                {
                    Vector3 basePosition = t.position;
                    Vector3 baseRotation = t.eulerAngles;
                    Vector3 baseScale = t.localScale;

                    for (int i = 1; i <= duplicateCount; i++)
                    {
                        GameObject clone = Instantiate(t.gameObject, t.parent);
                        Undo.RegisterCreatedObjectUndo(clone, "Duplicate Transform");

                        clone.transform.position = basePosition + positionEdit * i;
                        clone.transform.eulerAngles = baseRotation + rotationEdit * i;
                        clone.transform.localScale = baseScale + scaleEdit * i;

                        clone.name = t.name + nameSuffix + i;
                    }
                }
            }

            Debug.Log($"[MapEventSceneEditor] Applied '{transformMode}' to {selectedObjects.Length} object(s).");
        }
    }
}
#endif
