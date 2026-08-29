using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Characters.Controllers;
using Characters.MovementSystems;
using Characters.SO.StatusEffectSO;
using Characters.StatusEffectSystems;
using Characters.StatusEffectSystems.StatusEffects;
using Manager;

namespace GameControl.EventMap
{
    public class PullingCircleMapEvent : BaseMapEvent, ISphereHitbox, IFixedUpdateable, ISpecialInteractionTarget
    {
        [SerializeField] private Transform firePoint;

        [Header("Hitbox Settings (Sphere)")]
        [SerializeField] private float sphereRadius = 6f;
        [SerializeField] private Vector2 sphereOffset = Vector2.zero;
        [SerializeField] private LayerMask hitLayer;

        [Header("Pull Settings")]
        [SerializeField] private float dmgCenterRadius = 1f;
        [SerializeField] float basePull = 8f; 
        [SerializeField] float stopDistance = 0.1f;
        [SerializeField] AnimationCurve pullCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] bool useExponential = true;
        private bool IsPerforming;

        [Header("Special Interaction (Devourer)")]
        [Tooltip("Off = this Black Hole has no Special Interaction: Devourer will not consume or explode it.")]
        [SerializeField] private bool enableSpecialInteraction = true;

        [SerializeField] private BlackHoleExplosionData specialInteraction = new();

        /// <summary>
        /// Numbers for the Black Hole Special Interaction (Design Ver 0.0.20).
        /// Defaults are the Small Black Hole row; Medium / Large are set per prefab in the Inspector.
        /// </summary>
        [Serializable]
        public class BlackHoleExplosionData
        {
            [Tooltip("Small 20 / Medium 30 / Large 40")]
            public float explodeRadius = 20f;

            [Tooltip("Small 50 / Medium 100 / Large 150")]
            public float explodeDamage = 50f;

            [Tooltip("Small 10 / Medium 20 / Large 30 - drains Break Point on top of the explode damage.")]
            public float heavyDamage = 10f;

            [Tooltip("Small 5s / Medium 7s / Large 9s")]
            public float stunDuration = 5f;

            [Tooltip("Small 12 / Medium 16 / Large 20")]
            public float knockbackDistance = 12f;

            public float knockbackDuration = 0.5f;

            [Tooltip("Layers caught in the explosion - \"everything surrounding\", so player AND enemies. "
                     + "Leave empty to reuse the Black Hole's own Hit Layer.")]
            public LayerMask affectedLayer;

            [Tooltip("Stun effect data applied to everything caught in the explosion.")]
            public StunEffectDataSo stunEffectData;

            [Tooltip("Buffs stripped from everything the explosion catches, applied BEFORE the stun. "
                     + "Bright2 parks Iron Body on itself with The Immovable, and Iron Body makes a target "
                     + "immune to stun - without cancelling it here the explosion's stun is swallowed and "
                     + "the boss can never be broken. Doc: some special interaction (Devourer) can cancel "
                     + "out this buff.")]
            public List<StatusEffectName> cancelledEffects = new() { StatusEffectName.IronBody };
        }

        private bool _specialInteractionTriggered;

        private static readonly Collider2D[] _hits = new Collider2D[64];
        private static readonly HashSet<BaseController> _explosionTargets = new();

        public HitboxType HitboxType => HitboxType.Sphere;
        public float Radius { get => sphereRadius; set => sphereRadius = value; }
        public Vector3 Offset { get => sphereOffset; set => sphereOffset = value; }
        
        private void OnEnable()
        {
            FixedUpdateManager.Instance.Register(this);
            _specialInteractionTriggered = false;

            // Black Hole prefabs carry no Collider2D, so nothing can find them by physics - they have to
            // announce themselves. Pooled events run this every time they are spawned.
            if (enableSpecialInteraction)
                SpecialInteractionRegistry.Register(this);
        }
        private void OnDisable()
        {
            FixedUpdateManager.Current?.Unregister(this);
            SpecialInteractionRegistry.Unregister(this);
            IsPerforming = false;
        }

        public override async UniTask PlayPreview()
        {
            if (previewEffect == null) return;
            previewEffect?.Play();
            notifyFeedback?.PlayFeedbacks();
            await UniTask.WaitWhile(
                () => previewEffect != null && previewEffect.IsAlive(true),
                cancellationToken: this.GetCancellationTokenOnDestroy()
            );
        }

        protected override void Perform()
        {
            IsPerforming = true;
        }

        private void OnDrawGizmos()
        {
            if (firePoint == null) return;
            var center = firePoint.TransformPoint(sphereOffset);
            Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
            Gizmos.DrawWireSphere(center, sphereRadius);
            
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawSphere(center, dmgCenterRadius);

            if (!enableSpecialInteraction) return;
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.35f);
            Gizmos.DrawWireSphere(center, specialInteraction.explodeRadius);
        }

        public void OnFixedUpdate()
        {
            if (!IsPerforming) return;
            //Pull
            PullObject();
            //Dmg on touch Center
            DamageOnTouch();
        }

        private float _lastTimeHit;
        private const float _hitPerSec = 0.5f;
        private void DamageOnTouch()
        {
            if (Time.time <= _lastTimeHit + _hitPerSec) return;
            
            Vector2 center = firePoint ? firePoint.TransformPoint(sphereOffset) : (Vector2)transform.TransformPoint(sphereOffset);
            var count = Physics2D.OverlapCircleNonAlloc(center, dmgCenterRadius, _hits, hitLayer);
            if (count == 0) return;

            for (var i = 0; i < count; i++)
            {
                var col = _hits[i];
                if (!col) continue;

                var go = col.gameObject;
                if (go == gameObject) continue;
                CombatManager.ApplyRawDamageTo(go, gameObject, mapEventId, damage);
                _lastTimeHit = Time.time;
                Debug.Log("Black Hole Attack");
            }
        }

        private void PullObject()
        {
            Vector2 center = firePoint ? firePoint.TransformPoint(sphereOffset) : (Vector2)transform.TransformPoint(sphereOffset);
            var count = Physics2D.OverlapCircleNonAlloc(center, sphereRadius, _hits, hitLayer);
            if (count == 0) return;

            for (var i = 0; i < count; i++)
            {
                var col = _hits[i];
                if (!col) continue;

                var go = col.gameObject;
                if (go == gameObject) continue;
                if (!CombatManager.TryGetCharacterFromCache(go, out var targetController)) continue;

                var mover = targetController.MovementSystem;
                if (!mover) continue;
                if (mover.IgnoreExternalPull) continue;

                Vector2 pos = mover.transform.position;
                var toCenter = center - pos;
                var dist = toCenter.magnitude;
                if (dist <= stopDistance) continue;
                var proximity = Mathf.InverseLerp(sphereRadius, 0f, dist);
                var curveMul = Mathf.Max(0f, pullCurve.Evaluate(proximity));
                var strength = basePull * curveMul;

                var dir = toCenter / dist;

                if (!useExponential)
                {
                    var step = strength * Time.fixedDeltaTime;
                    var nextPos = pos + dir * Mathf.Min(step, dist - stopDistance);
                    mover.TryMoveRawPosition(nextPos);
                }
                else
                {
                    var t = 1f - Mathf.Exp(-strength * Time.fixedDeltaTime);
                    var lerped = Vector2.Lerp(pos, center, t);
                    if ((lerped - center).sqrMagnitude < stopDistance * stopDistance)
                        lerped = center + (pos - center).normalized * stopDistance;

                    mover.TryMoveRawPosition(lerped);
                }
            }
        }

        #region Special Interaction

        public Transform Transform => transform;

        public bool CanTriggerSpecialInteraction =>
            enableSpecialInteraction && !_specialInteractionTriggered && isActiveAndEnabled;

        /// <summary>
        /// Devourer pulled this Black Hole into its damage radius: the Black Hole disappears and leaves
        /// an explosion that damages, knocks back and stuns everything around it (Design Ver 0.0.20).
        /// </summary>
        public void TriggerSpecialInteraction(SpecialInteractionContext context)
        {
            if (!CanTriggerSpecialInteraction) return;
            _specialInteractionTriggered = true;
            SpecialInteractionRegistry.Unregister(this);

            // Stop pulling the moment it is consumed, so it can't keep dragging things for the frames
            // between here and the pool release.
            IsPerforming = false;

            Explode(GetCenter());
            EndAndRelease();
        }

        private Vector2 GetCenter() =>
            firePoint ? firePoint.TransformPoint(sphereOffset) : (Vector2)transform.TransformPoint(sphereOffset);

        private void Explode(Vector2 center)
        {
            // Unset explosion layer falls back to the layer this Black Hole already pulls and damages,
            // so a prefab that only fills in the numbers still explodes at the right things.
            LayerMask explodeLayer = specialInteraction.affectedLayer.value != 0
                ? specialInteraction.affectedLayer
                : hitLayer;

            var hits = Physics2D.OverlapCircleAll(center, specialInteraction.explodeRadius, explodeLayer);
            if (hits.Length == 0) return;

            _explosionTargets.Clear();

            foreach (var col in hits)
            {
                if (!col) continue;
                if (!CombatManager.TryGetCharacterFromCache(col.gameObject, out var target)) continue;
                if (!_explosionTargets.Add(target)) continue; // a character can own several colliders
                if (target.HealthSystem == null || target.HealthSystem.IsDead) continue;

                CombatManager.ApplyRawDamageTo(target.gameObject, gameObject, mapEventId,
                    specialInteraction.explodeDamage, specialInteraction.heavyDamage);

                // Strip first: a target still holding Iron Body would shrug the stun off entirely.
                CancelBuffs(target);
                ApplyKnockback(target, center);
                ApplyStun(target);
            }

            _explosionTargets.Clear();
        }

        private void CancelBuffs(BaseController target)
        {
            var cancelled = specialInteraction.cancelledEffects;
            if (cancelled == null) return;

            for (int i = 0; i < cancelled.Count; i++)
                StatusEffectManager.RemoveEffectAt(target.gameObject, cancelled[i]);
        }

        private void ApplyKnockback(BaseController target, Vector2 center)
        {
            if (specialInteraction.knockbackDistance <= 0f) return;
            if (!target.MovementSystem) return;

            Vector2 position = target.transform.position;
            Vector2 direction = position - center;

            // Dead centre of the blast has no outward direction - push it somewhere rather than nowhere.
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;

            target.MovementSystem.TryMoveToPositionOverTime(
                position + direction * specialInteraction.knockbackDistance,
                specialInteraction.knockbackDuration);
        }

        private void ApplyStun(BaseController target)
        {
            if (!specialInteraction.stunEffectData || specialInteraction.stunDuration <= 0f) return;
            if (target.StatusEffectSystem == null) return;

            // Same construction path as BreakPointSystem: build the effect directly so the per-Black-Hole
            // duration wins over the one baked into the shared effect data.
            var effect = (BaseStatusEffect)Activator.CreateInstance(specialInteraction.stunEffectData.EffectType);
            effect.AssignEffectData(specialInteraction.stunEffectData, specialInteraction.stunDuration);
            target.StatusEffectSystem.AddEffect(effect);
        }

        #endregion
    }
}
