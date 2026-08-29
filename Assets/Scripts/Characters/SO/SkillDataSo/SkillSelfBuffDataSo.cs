using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    /// <summary>
    /// A skill whose entire effect is "buff the caster" - it has a single Activate state and no behaviour
    /// of its own. Bright2's <b>The Immovable</b> (Iron Body) and <b>Perfect shape</b> (Damage resistance)
    /// are both exactly this, and so is anything else that just parks a status effect on its owner.
    /// <para/>
    /// Everything it needs already lives on <see cref="BaseSkillDataSo"/>, so this type adds no fields:
    /// <list type="bullet">
    /// <item><b>Status Effects -> Status Effect On Skill Start</b> - the buff(s) to grant. Tick Override
    /// Duration and set it (the doc uses 999) so the buff outlives the cast.</item>
    /// <item><b>Status Effects -> Clear Buff On Skill Exit</b> - leave OFF, or the buff is stripped again
    /// the instant the cast ends.</item>
    /// <item><b>Cooldown</b> and <b>Activate Radius</b> - how often, and from how far, the owner re-buffs.</item>
    /// </list>
    /// </summary>
    [CreateAssetMenu(fileName = "SkillSelfBuffData", menuName = "GameData/SkillData/SelfBuff")]
    public class SkillSelfBuffDataSo : BaseSkillDataSo
    {
    }
}
