#if UNITY_EDITOR
using System;
using System.Linq;
using Characters.SO.StatusEffectMetaSO;
using Characters.StatusEffectSystem.DrawerSettings;
using Characters.StatusEffectSystem.StatusEffects;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;

public class StatusEffectParamDrawerAttributeDrawer : OdinAttributeDrawer<StatusEffectParamDrawerAttribute, BaseStatusEffect>
{
    private Type lastClassType = null;

    protected override void DrawPropertyLayout(GUIContent label)
    {
        var parent = this.Property.Parent;
        var metaProp = parent.Children.FirstOrDefault(p => p.Name == "meta");

        if (metaProp?.ValueEntry?.WeakSmartValue is not StatusEffectMetaDataSo meta)
        {
            SirenixEditorGUI.ErrorMessageBox("Select a valid StatusEffectMetaDataSo first.");
            return;
        }

        var classType = meta.ClassType;

        if (classType == null || !typeof(BaseStatusEffect).IsAssignableFrom(classType))
        {
            SirenixEditorGUI.ErrorMessageBox("Invalid or missing Class Type in MetaData.");
            return;
        }

        if (classType.IsAbstract || classType.GetConstructor(Type.EmptyTypes) == null)
        {
            SirenixEditorGUI.ErrorMessageBox($"Runtime type {classType.Name} must be instantiable (concrete + no-arg constructor).\"");
            return;
        }

        // Auto-create or update effectParams when meta changes
        if (lastClassType != classType || Property.ValueEntry.WeakSmartValue == null || Property.ValueEntry.TypeOfValue != classType)
        {
            Property.ValueEntry.WeakSmartValue = Activator.CreateInstance(classType);
            lastClassType = classType;
        }

        this.CallNextDrawer(label);
    }
}
#endif