using UnityEngine;

namespace Characters.SO.StatusEffectSO
{
    /// <summary>
    /// Data for the debuff applied to an enemy while it's being pulled by a Gravity Orb.
    /// While active, the target cannot use its Primary or Secondary skill (Auto skills are unaffected).
    /// Duration is managed manually by the orb (applied on pull-field enter, removed on exit / orb end),
    /// so set "Default Duration" high (or always use an Override Duration on the payload that references
    /// this data) to make sure it never expires early while the target is still being held.
    /// </summary>
    [CreateAssetMenu(fileName = "GravityPulledEffectData", menuName = "GameData/StatusEffectData/GravityPulledEffectData")]
    public class GravityPulledEffectDataSo : BaseStatusEffectDataSo
    {
    }
}
