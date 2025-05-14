#if UNITY_EDITOR
using System;
using System.Linq;
using Characters.SO.StatusEffectMetaSO;
using Characters.StatusEffectSystem.StatusEffectRuntimes;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Characters.StatusEffectSystem.DrawerSettings
{
    public class StatusEffectParamDrawerAttributeDrawer : OdinAttributeDrawer<StatusEffectParamDrawerAttribute, BaseStatusEffectRuntime>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var parent = this.Property.Parent;
            var metaProp = parent.Children.FirstOrDefault(p => p.Name == "statusEffectMeta");

            if (metaProp?.ValueEntry?.WeakSmartValue is not StatusEffectMetaDataSo meta)
            {
                SirenixEditorGUI.ErrorMessageBox("Select a valid StatusEffectMetaDataSo first.");
                return;
            }

            var runtimeType = meta.Runtime;

            if (runtimeType == null || !typeof(BaseStatusEffectRuntime).IsAssignableFrom(runtimeType))
            {
                SirenixEditorGUI.ErrorMessageBox("Invalid or missing Runtime Type in MetaData.");
                return;
            }

            if (runtimeType.IsAbstract || runtimeType.GetConstructor(Type.EmptyTypes) == null)
            {
                SirenixEditorGUI.ErrorMessageBox($"Runtime type {runtimeType.Name} must be instantiable (concrete + no-arg constructor).\"");
                return;
            }

            if (Property.ValueEntry.WeakSmartValue == null || Property.ValueEntry.TypeOfValue != runtimeType)
            {
                Property.ValueEntry.WeakSmartValue = Activator.CreateInstance(runtimeType);
            }

            CallNextDrawer(label);
        }
    }
}
#endif