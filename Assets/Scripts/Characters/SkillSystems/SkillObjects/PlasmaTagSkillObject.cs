using System;
using System.Threading;
using Characters.Controllers;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Characters.SkillSystems.SkillObjects
{
    /// <summary>
    /// Snapshot of everything a bomb needs to run its lifecycle.
    /// Passed by value so it stays valid even if the runtime is destroyed mid-flight.
    /// </summary>
    public readonly struct PlasmaBombConfig
    {
        public readonly GameObject OwnerGo;
        public readonly string OwnerId;
        public readonly float Countdown;
        public readonly float DamageDuration;
        public readonly float CleanupDelay;
        public readonly float BaseDamage;
        public readonly float DamageMultiplier;
        public readonly bool ExplodeOnTargetDead;

        public PlasmaBombConfig(
            GameObject ownerGo, string ownerId,
            float countdown, float damageDuration, float cleanupDelay,
            float baseDamage, float damageMultiplier, bool explodeOnTargetDead)
        {
            OwnerGo = ownerGo;
            OwnerId = ownerId;
            Countdown = countdown;
            DamageDuration = damageDuration;
            CleanupDelay = cleanupDelay;
            BaseDamage = baseDamage;
            DamageMultiplier = damageMultiplier;
            ExplodeOnTargetDead = explodeOnTargetDead;
        }
    }

    /// <summary>
    /// Bomb lifecycle: Idle -> Counting -> Exploding -> (caller resets + releases to pool).
    /// Explosion can start from the countdown finishing OR from <see cref="ForceDetonate"/>.
    /// </summary>
    public class PlasmaTagSkillObject : BaseSkillObject
    {
        public enum BombState { Idle, Counting, Exploding }

        [SerializeField] private TextMeshPro countDownText;

        [Tooltip("Optional. Visual of the armed bomb, hidden when it detonates.")]
        [SerializeField] private GameObject bombVisual;

        [Tooltip("Plays while counting down; stopped and hidden when the bomb detonates.")]
        [SerializeField] private VfxGroup chargeVfx;

        [Tooltip("Plays on detonation only (countdown finished or ForceDetonate).")]
        [SerializeField] private VfxGroup explosionVfx;

        /// <summary>
        /// A VFX root plus its particle systems. Play() explicitly activates the root and plays every
        /// ParticleSystem under it, so it works whether or not "Play On Awake" is ticked.
        /// </summary>
        [Serializable]
        private class VfxGroup
        {
            [SerializeField] private GameObject root;
            private ParticleSystem[] _systems;

            private ParticleSystem[] Systems =>
                _systems ??= root.GetComponentsInChildren<ParticleSystem>(true);

            public void Play()
            {
                if (!root) return;
                root.SetActive(true);
                foreach (var ps in Systems)
                {
                    ps.Clear(false);
                    ps.Play(false);
                }
            }

            public void Stop()
            {
                if (!root) return;
                foreach (var ps in Systems)
                    ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                root.SetActive(false);
            }
        }

        private BaseController _target;
        private bool _explodeOnTargetDead;
        private bool _detonateRequested;

        public BombState State { get; private set; } = BombState.Idle;

        /// <summary>True from the moment the bomb is armed until ResetForPool.</summary>
        public bool IsBusy => State != BombState.Idle;

        /// <summary>The controller this bomb is attached to. Null if not following anyone.</summary>
        public BaseController Target => _target;

        private bool IsAlive => this && gameObject.activeSelf;

        private void Update()
        {
            if (!_target) return;
            transform.position = _target.transform.position;
        }

        // ======================== LIFECYCLE ========================

        /// <summary>
        /// Runs the whole bomb lifecycle: countdown -> explosion (damage + VFX) -> cleanup delay.
        /// Returns when the explosion has fully finished. The caller is responsible for
        /// ResetForPool + releasing to the pool afterwards.
        /// Uses destroyCancellationToken: only fires on Destroy(), not SetActive(false).
        /// </summary>
        public async UniTask RunAsync(BaseController target, PlasmaBombConfig cfg)
        {
            var ct = destroyCancellationToken;

            Arm(target, cfg.ExplodeOnTargetDead);

            await CountdownAsync(cfg.Countdown, ct);
            if (!IsAlive) return;

            await ExplodeAsync(cfg, ct);
        }

        /// <summary>
        /// Skip the remaining countdown and explode on the next frame.
        /// No-op unless the bomb is currently counting down.
        /// Only sets a flag, so it is safe to call from inside event callbacks (e.g. OnDead).
        /// </summary>
        public void ForceDetonate()
        {
            if (State == BombState.Counting)
                _detonateRequested = true;
        }

        private void Arm(BaseController target, bool explodeOnTargetDead)
        {
            State = BombState.Counting;
            _detonateRequested = false;
            _explodeOnTargetDead = explodeOnTargetDead;

            explosionVfx.Stop();
            if (bombVisual) bombVisual.SetActive(true);
            chargeVfx.Play();

            FollowTarget(target);
        }

        private async UniTask CountdownAsync(float duration, CancellationToken ct)
        {
            float remaining = duration;

            while (remaining > 0f && !_detonateRequested)
            {
                if (countDownText)
                    countDownText.text = remaining.ToString("F1");

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                remaining -= Time.deltaTime;
            }

            if (countDownText)
                countDownText.text = "";
        }

        private async UniTask ExplodeAsync(PlasmaBombConfig cfg, CancellationToken ct)
        {
            State = BombState.Exploding;

            // Freeze at the current position (target may be dead / about to be recycled).
            StopFollow();

            if (bombVisual) bombVisual.SetActive(false);
            chargeVfx.Stop();
            explosionVfx.Play();

            // Bomb itself is the damage source key (passing null caused a damage regression).
            DamageOnTouch.EnableDamage(cfg.OwnerGo, cfg.OwnerId, this, 1, cfg.BaseDamage, cfg.DamageMultiplier);

            await UniTask.WaitForSeconds(cfg.DamageDuration, cancellationToken: ct);
            if (!IsAlive) return;

            DamageOnTouch.DisableDamage(this);

            // Let the VFX finish before the caller deactivates us.
            if (cfg.CleanupDelay > 0f)
                await UniTask.WaitForSeconds(cfg.CleanupDelay, cancellationToken: ct);
        }

        // ======================== TARGET FOLLOW ========================

        private void FollowTarget(BaseController target)
        {
            StopFollow();
            _target = target;
            if (_target != null)
                _target.HealthSystem.OnDead += OnTargetDead;
        }

        public void StopFollow()
        {
            if (_target == null) return;
            _target.HealthSystem.OnDead -= OnTargetDead;
            _target = null;
        }

        private void OnTargetDead()
        {
            StopFollow();
            if (_explodeOnTargetDead)
                ForceDetonate();
        }

        // ======================== VFX / POOL ========================

        /// <summary>Full reset before returning to pool.</summary>
        public void ResetForPool()
        {
            StopFollow();
            DamageOnTouch.DisableDamage(this);

            State = BombState.Idle;
            _detonateRequested = false;

            if (countDownText) countDownText.text = "";
            if (bombVisual) bombVisual.SetActive(true);
            chargeVfx.Stop();
            explosionVfx.Stop();
        }
    }
}