using System;
using System.Collections.Generic;
using System.Threading;
using Characters.Controllers;
using Characters.SO.SkillDataSo;
using Characters.StatusEffectSystems.StatusEffects;
using Cysharp.Threading.Tasks;
using Manager;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    /// <summary>
    /// Runtime for Bright2's "Singularity" (phase 3): a low cost time stop.
    /// <para/>
    /// Pulses on a timer for the skill's duration; each pulse stuns every character inside the radius for
    /// a short beat. Because the stun outlasts the gap between pulses, anyone who stays inside is held for
    /// the whole skill, while anyone who gets out recovers shortly after - no bookkeeping needed, the
    /// status effect's own timer does the work.
    /// </summary>
    public class SkillSingularityRuntime : BaseSkillRuntime<SkillSingularityDataSo>
    {
        private readonly Collider2D[] _hitBuffer = new Collider2D[64];
        private readonly HashSet<BaseController> _pulseTargets = new();

        protected override void OnSkillStart()
        {
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            if (!skillData.StunEffectData)
            {
                Debug.LogWarning("[Singularity] No stun effect data assigned - the skill will do nothing.", this);
                return;
            }

            float interval = 1f / Mathf.Max(0.01f, skillData.TriggerPerSecond);
            float elapsed = 0f;

            // Pulse immediately, then keep pulsing until the duration runs out.
            while (elapsed <= skillData.Duration)
            {
                cancelToken.ThrowIfCancellationRequested();
                if (!owner) return;

                Pulse();

                await UniTask.WaitForSeconds(interval, cancellationToken: cancelToken);
                elapsed += interval;
            }
        }

        protected override void OnSkillExit()
        {
            _pulseTargets.Clear();
        }

        /// <summary>Stuns everything currently inside the radius. Nothing is remembered between pulses.</summary>
        private void Pulse()
        {
            Vector2 center = owner.transform.position;
            int found = Physics2D.OverlapCircleNonAlloc(center, skillData.SkillRadius, _hitBuffer,
                skillData.AffectedLayer);
            if (found <= 0) return;

            _pulseTargets.Clear();

            for (int i = 0; i < found; i++)
            {
                var col = _hitBuffer[i];
                if (!col) continue;
                if (!CombatManager.TryGetCharacterFromCache(col.gameObject, out var target)) continue;
                if (target == owner) continue; // the singularity does not stop itself
                if (!_pulseTargets.Add(target)) continue; // a character can own several colliders
                if (target.HealthSystem == null || target.HealthSystem.IsDead) continue;
                if (target.StatusEffectSystem == null) continue;
                if (!IsAffected(target)) continue;

                ApplyStun(target);
            }
        }

        /// <summary>
        /// Decided per character rather than by layer, so the switches keep working regardless of how the
        /// scan layer is configured.
        /// </summary>
        private bool IsAffected(BaseController target)
            => target is PlayerController ? skillData.AffectPlayer : skillData.AffectEnemies;

        /// <summary>
        /// Builds the stun directly so this skill's own duration wins over the one baked into the shared
        /// stun data - same approach BreakPointSystem and the Black Hole interaction use.
        /// </summary>
        private void ApplyStun(BaseController target)
        {
            var effect = (BaseStatusEffect)Activator.CreateInstance(skillData.StunEffectData.EffectType);
            effect.AssignEffectData(skillData.StunEffectData, skillData.StunDuration);
            target.StatusEffectSystem.AddEffect(effect);
        }

        private void OnDrawGizmos()
        {
            if (!IsPerforming || skillData == null || !owner) return;

            Gizmos.color = new Color(0.7f, 0.3f, 1f, 0.9f);
            Gizmos.DrawWireSphere(owner.transform.position, skillData.SkillRadius);
        }
    }
}
