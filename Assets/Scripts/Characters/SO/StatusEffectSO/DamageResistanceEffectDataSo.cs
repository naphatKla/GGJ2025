using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.StatusEffectSO
{
    /// <summary>
    /// Data for the Damage Resistance buff - the target ignores a percentage of the HP damage it takes.
    /// Granted by Bright2's <b>Perfect shape</b> at 100% (Design Ver 0.0.20), which makes the boss immune
    /// to ordinary damage until a Devourer Special Interaction cancels the buff.
    /// <para/>
    /// Heavy damage is deliberately unaffected: it drains Break Point rather than HP, so breaking the boss
    /// stays possible while this is up. That is what stops 100% resistance from being a dead end.
    /// </summary>
    [CreateAssetMenu(fileName = "DamageResistanceEffectData",
        menuName = "GameData/StatusEffectData/DamageResistanceEffectData")]
    public class DamageResistanceEffectDataSo : BaseStatusEffectDataSo
    {
        [Title("Damage Resistance")]
        [Unit(Units.Percent)]
        [PropertyRange(0, 100)]
        [PropertyTooltip("Percentage of incoming HP damage ignored while this buff is active. Spec: 100%")]
        [SerializeField]
        private float resistancePercentage = 100f;

        public float ResistancePercentage => resistancePercentage;
    }
}
