using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Khami.SearchableDropdown.Editor
{
    [CustomPropertyDrawer(typeof(SearchableDropdownAttribute))]
    public class SearchableDropdownDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (SearchableDropdownAttribute)attribute;
        
            EditorGUI.BeginProperty(position, label, property);
        
            var fieldPosition = EditorGUI.PrefixLabel(position, label);
            var currentValue = property.stringValue;

            if (EditorGUI.DropdownButton(fieldPosition, new GUIContent(currentValue), FocusType.Keyboard))
            {
                ShowSearchWindow(fieldPosition, property, attr.MethodName);
            }

            EditorGUI.EndProperty();
        }

        private void ShowSearchWindow(Rect fieldPosition, SerializedProperty property, string methodName)
        {
            var targetObject = property.serializedObject.targetObject;
            var method = targetObject.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null) return;
            var options = method.Invoke(targetObject, null) as IEnumerable<string>;
            if (options != null)
            {
                SearchablePopup.Show(fieldPosition, property, options);
            }
        }
    }
}