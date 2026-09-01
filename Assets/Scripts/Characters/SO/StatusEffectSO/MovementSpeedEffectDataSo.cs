using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.StatusEffectSO
{
    /// <summary>
    /// Data for a flat movement-speed change applied as a status effect. Negative values slow the target -
    /// Bright2's <b>Piece of Mine</b> uses -80% for 5s when a fragment reaches the player.
    /// <para/>
    /// Not to be confused with <c>LV4_MovementSpeed</c>, which despite its name is a FlowState buff that
    /// speeds the player UP. This is the plain, standalone speed modifier.
    /// </summary>
    [CreateAssetMenu(fileName = "MovementSpeedEffectData",
        menuName = "GameData/StatusEffectData/MovementSpeedEffectData")]
    public class MovementSpeedEffectDataSo : BaseStatusEffectDataSo
    {
        [Title("Movement Speed")]
        [Unit(Units.Percent)]
        [PropertyRange(-100, 200)]
        [PropertyTooltip("Change to the target's movement speed while active. Negative slows it down: "
                         + "-80 means the target keeps 20% of its speed. Piece of Mine spec: -80%")]
        [SerializeField]
        private float speedPercentChange = -80f;

        public float SpeedPercentChange => speedPercentChange;
    }
}
