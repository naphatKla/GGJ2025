using UnityEngine;

namespace Characters.SO.StatusEffectSO
{
    /// <summary>
    /// ScriptableObject that defines metadata for the Invincible status effect.
    /// Inherits all common properties (icon, duration, level, etc.) from BaseStatusEffectDataSo.
    /// </summary>
    [CreateAssetMenu(fileName = "IframeEffectData", menuName = "GameData/StatusEffectData/IframeEffectData")]
    public class IframeEffectDataSo : BaseStatusEffectDataSo
    {
        // Currently no additional fields. All configuration is inherited.
    }
}