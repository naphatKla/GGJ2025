using System.Threading;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;

namespace Characters.SkillSystems.SkillRuntimes
{
    /// <summary>
    /// Runtime for <see cref="SkillSelfBuffDataSo"/> - a skill that only buffs its caster.
    /// <para/>
    /// There is deliberately nothing here: <c>BaseSkillRuntime.HandleSkillStart</c> already applies the
    /// skill's Status Effect On Skill Start to the owner, and <c>HandleSkillExit</c> already strips them
    /// again when Clear Buff On Skill Exit is ticked. This runtime just starts and finishes, letting the
    /// generic skill lifecycle do the work.
    /// <para/>
    /// Used by Bright2's The Immovable (Iron Body) and Perfect shape (Damage resistance).
    /// </summary>
    public class SkillSelfBuffRuntime : BaseSkillRuntime<SkillSelfBuffDataSo>
    {
        protected override void OnSkillStart()
        {
        }

        /// <summary>Instant: the buff is granted by the shared skill start handling, then the cast is over.</summary>
        protected override UniTask OnSkillUpdate(CancellationToken cancelToken) => UniTask.CompletedTask;

        protected override void OnSkillExit()
        {
        }
    }
}
