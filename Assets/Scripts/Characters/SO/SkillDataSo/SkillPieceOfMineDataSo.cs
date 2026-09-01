using System.Collections.Generic;
using Characters.StatusEffectSystems;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    /// <summary>
    /// Data for Bright2's "Piece of Mine" (Design Ver 0.0.20) - the boss sheds pieces of itself that chase
    /// the player down and act like ordinary enemies.
    /// <para/>
    /// Everything about spawning them (how many, where, pooling, spread) is inherited from
    /// <see cref="SkillEnemySummonDataSo"/>. What this type adds is what happens when a piece actually
    /// reaches the player: it lands its hit, slows them, and is gone.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillPieceOfMineData", menuName = "GameData/SkillData/TheBright2Only/PieceOfMine")]
    public class SkillPieceOfMineDataSo : SkillEnemySummonDataSo
    {
        [FoldoutGroup("On Contact")]
        [PropertyTooltip("Applied to the player when a fragment touches them. Spec: decrease movement speed "
                         + "by 80% for 5s - use a MovementSpeedEffectData with Override Duration 5.")]
        [SerializeField] private List<StatusEffectDataPayload> contactStatusEffects;

        [FoldoutGroup("On Contact")]
        [LabelText("Despawn On Contact")]
        [PropertyTooltip("Spec: the fragment disappears once it has hit the player. Untick to let pieces "
                         + "keep chasing after landing a hit.")]
        [SerializeField] private bool despawnOnContact = true;

        public List<StatusEffectDataPayload> ContactStatusEffects => contactStatusEffects;
        public bool DespawnOnContact => despawnOnContact;
    }
}
