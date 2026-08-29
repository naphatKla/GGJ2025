using System.Collections.Generic;
using Characters.Controllers;
using UnityEngine;

namespace GameControl.EventMap
{
    /// <summary>
    /// Everything a Special Interaction needs to know about whoever set it off.
    /// </summary>
    public readonly struct SpecialInteractionContext
    {
        /// <summary>Character whose skill absorbed the target (Bright2, for Devourer). May be null.</summary>
        public readonly BaseController Instigator;

        /// <summary>Object credited as the attacker for any damage the interaction deals.</summary>
        public readonly GameObject InstigatorObject;

        /// <summary>Attacker id used for hit bookkeeping (hit cooldown is tracked per attacker id).</summary>
        public readonly string InstigatorId;

        /// <summary>Centre of the absorbing skill at the moment the interaction fired.</summary>
        public readonly Vector2 Origin;

        public SpecialInteractionContext(BaseController instigator, GameObject instigatorObject,
            string instigatorId, Vector2 origin)
        {
            Instigator = instigator;
            InstigatorObject = instigatorObject;
            InstigatorId = instigatorId;
            Origin = origin;
        }
    }

    /// <summary>
    /// "Special Interaction" (Design Ver 0.0.20) — a reaction that fires when a specific object gets
    /// pulled into a skill that cares about it. Today only Devourer triggers these.
    /// <para/>
    /// The reaction lives on the <b>target</b>, not on the skill, because the doc defines its numbers
    /// per object type (Small / Medium / Large Black Hole each explode differently). A skill only has to
    /// ask <see cref="SpecialInteractionRegistry"/> what is within reach and call
    /// <see cref="TriggerSpecialInteraction"/> — it never needs to know what the reaction actually does.
    /// <para/>
    /// Implementing this interface IS the opt-in: a map event that doesn't implement it can never be
    /// devoured, so no separate whitelist is needed anywhere.
    /// </summary>
    public interface ISpecialInteractionTarget
    {
        /// <summary>
        /// Position source for the range check. Map events carry no colliders, so the registry cannot
        /// fall back on physics for this.
        /// </summary>
        Transform Transform { get; }

        /// <summary>False once it has already fired, or while the target isn't in a state to react.</summary>
        bool CanTriggerSpecialInteraction { get; }

        void TriggerSpecialInteraction(SpecialInteractionContext context);
    }

    /// <summary>
    /// Tracks every live <see cref="ISpecialInteractionTarget"/> so skills can find them by distance.
    /// <para/>
    /// This exists because map event prefabs (Black Hole included) have no Collider2D at all — they
    /// query physics, nothing queries them — so an OverlapCircle sweep would never see them no matter
    /// what layer they sit on. Targets register on enable and unregister on disable; because they are
    /// pooled, both happen every time one is spawned or recycled.
    /// </summary>
    public static class SpecialInteractionRegistry
    {
        private readonly struct Entry
        {
            public readonly ISpecialInteractionTarget Target;

            /// <summary>Cached so a destroyed target can be swept without touching the dead component.</summary>
            public readonly Transform Transform;

            public Entry(ISpecialInteractionTarget target)
            {
                Target = target;
                Transform = target.Transform;
            }
        }

        private static readonly List<Entry> Entries = new();

        public static void Register(ISpecialInteractionTarget target)
        {
            if (target == null || target.Transform == null) return;

            for (int i = 0; i < Entries.Count; i++)
                if (ReferenceEquals(Entries[i].Target, target))
                    return;

            Entries.Add(new Entry(target));
        }

        public static void Unregister(ISpecialInteractionTarget target)
        {
            if (target == null) return;

            for (int i = Entries.Count - 1; i >= 0; i--)
                if (ReferenceEquals(Entries[i].Target, target))
                    Entries.RemoveAt(i);
        }

        /// <summary>
        /// Fills <paramref name="results"/> with every registered target within <paramref name="radius"/>
        /// of <paramref name="origin"/> that is currently able to fire. The caller gets its own list, so
        /// it is safe to trigger the results even though triggering usually unregisters them.
        /// </summary>
        public static void GetTargetsInRadius(Vector2 origin, float radius,
            List<ISpecialInteractionTarget> results)
        {
            results.Clear();

            float sqrRadius = radius * radius;

            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                var entry = Entries[i];

                // Destroyed without a matching Unregister (scene unload, Destroy on a pooled object).
                if (entry.Transform == null)
                {
                    Entries.RemoveAt(i);
                    continue;
                }

                if (!entry.Target.CanTriggerSpecialInteraction) continue;
                if (((Vector2)entry.Transform.position - origin).sqrMagnitude > sqrRadius) continue;

                results.Add(entry.Target);
            }
        }
    }
}
