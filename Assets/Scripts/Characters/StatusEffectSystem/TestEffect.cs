using System.Collections.Generic;
using Characters.StatusEffectSystem;
using Manager;
using Sirenix.OdinInspector;
using UnityEngine;

public class TestEffect : MonoBehaviour
{
    [SerializeField] private List<StatusEffectDataPayload> _effectDataPayloads;

    [Title("")] [Button]
    public async void SendEffectToTarget(GameObject target)
    {
        foreach (var effect in _effectDataPayloads)
        {
            StatusEffectManager.ApplyEffectTo(target, effect);
        }
    }
}
