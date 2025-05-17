using UnityEngine;

namespace Characters.SO.StatusEffectSO
{
    /// <summary>
    /// ScriptableObject that defines metadata for the Invincible status effect.
    /// Inherits all common properties (icon, duration, level, etc.) from BaseStatusEffectDataSo.
    /// </summary>
    [CreateAssetMenu(fileName = "InvincibleEffectData", menuName = "GameData/StatusEffectData/InvincibleEffectData")]
    public class InvincibleEffectDataSo : BaseStatusEffectDataSo
    {
        // Currently no additional fields. All configuration is inherited.
    }
}