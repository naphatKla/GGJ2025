using Characters.Controllers;
using Characters.SO.SkillDataSo;

namespace Characters.SkillSystems.SkillRuntimes
{
    /// <summary>
    /// Runtime for Bright2's "Piece of Mine". Spawning is entirely
    /// <see cref="SkillEnemySummonRuntime"/>'s job - this only gives each fragment the on-contact
    /// slow-and-vanish behaviour the spec asks for.
    /// <para/>
    /// The behaviour is attached at runtime rather than baked into the fragment prefab, so the same prefab
    /// can be summoned by other skills without dragging Piece of Mine's rules along with it.
    /// </summary>
    public class SkillPieceOfMineRuntime : SkillEnemySummonRuntime
    {
        private SkillPieceOfMineDataSo PieceData => skillData as SkillPieceOfMineDataSo;

        protected override void OnSummonSpawned(EnemyController summon)
        {
            base.OnSummonSpawned(summon);

            var data = PieceData;
            if (data == null || !summon) return;

            var fragment = summon.GetComponent<PieceOfMineFragment>();
            if (fragment == null)
                fragment = summon.gameObject.AddComponent<PieceOfMineFragment>();

            fragment.Bind(summon, data.ContactStatusEffects, data.DespawnOnContact);
        }
    }
}
