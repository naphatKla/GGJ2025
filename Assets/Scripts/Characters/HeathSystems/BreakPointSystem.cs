using System;
using System.Threading;
using Characters.Controllers;
using Characters.FeedbackSystems;
using Characters.SO.StatusEffectSO;
using Characters.StatusEffectSystems.StatusEffects;
using Cysharp.Threading.Tasks;
using Manager;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.HeathSystems
{
    /// <summary>
    /// Break Point system (Bright2 spec — Design Ver 0.0.20).
    /// <para/>
    /// Heavy attacks reduce Break Point instead of HP (routing handled by <see cref="CombatManager"/>).
    /// When Break Point reaches 0 the owner enters the <b>Breaking</b> state:
    /// <list type="bullet">
    /// <item>Cancels all performing skills completely.</item>
    /// <item>Stuns the owner (default 7s) — respects Iron Body like normal stun.</item>
    /// <item>Takes the last committed ACTUAL hit damage x N more (default x10).</item>
    /// <item>Restores Break Point back to full when the Breaking state ends.</item>
    /// </list>
    /// Attach this component next to a <see cref="HealthSystem"/> on bosses that support Break Point.
    /// </summary>
    public class BreakPointSystem : MonoBehaviour
    {
        #region Inspector & Variables

        [Title("Break Point Configs")]
        [SerializeField] private float maxBreakPoint = 100f;

        [Unit(Units.Second)]
        [FoldoutGroup("Breaking Configs"), SerializeField]
        private float breakingStunDuration = 7f;

        [FoldoutGroup("Breaking Configs"), SerializeField]
        private int breakingDamageRepeatMultiplier = 10;

        [Title("Dependents")]
        [PropertyTooltip("Stun effect data applied when entering the Breaking state.")]
        [SerializeField] private StunEffectDataSo breakingStunEffectData;

        protected BaseController owner;
        private HealthSystem _healthSystem;

        [ShowInInspector, ReadOnly]
        [ShowIf("@UnityEngine.Application.isPlaying")]
        private float _currentBreakPoint;

        [ShowInInspector, ReadOnly]
        [ShowIf("@UnityEngine.Application.isPlaying")]
        private bool _isBreaking;

        /// <summary>Last ACTUAL (non-heavy) hit that was committed — used for the x N repeat damage.</summary>
        private float _lastActualHitDamage;

        private CancellationTokenSource _breakingCts;

        #endregion

        #region Events & Properties

        /// <summary>(currentBreakPoint, maxBreakPoint)</summary>
        public event Action<float, float> OnBreakPointChanged;

        /// <summary>Invoked once when Break Point reaches 0 (entering Breaking state).</summary>
        public event Action OnBreakingStart;

        /// <summary>Invoked once when Breaking ends (stun finished, Break Point restored).</summary>
        public event Action OnBreakingEnd;

        public bool IsBreaking => _isBreaking;
        public float CurrentBreakPoint => _currentBreakPoint;
        public float MaxBreakPoint => maxBreakPoint;
        public float BreakPointPercentage01 => maxBreakPoint > 0 ? Mathf.Clamp01(_currentBreakPoint / maxBreakPoint) : 0f;

        #endregion

        #region Public API

        /// <summary>Assigns break point data. Call during initialization (e.g. from the boss controller).</summary>
        public void AssignBreakPointData(BaseController ownerController, float newMaxBreakPoint)
        {
            owner = ownerController;
            maxBreakPoint = Mathf.Max(1f, newMaxBreakPoint);
            _healthSystem = owner ? owner.HealthSystem : null;

            if (_healthSystem != null)
                _healthSystem.OnHit += HandleOnHit;

            ResetBreakPointSystem();
        }

        /// <summary>
        /// Core heavy damage entry point. Reduces Break Point by <see cref="HealthSystem.HitInfo.heavyDamage"/>.
        /// Returns false if the hit was ignored (dead / currently breaking / no heavy damage).
        /// </summary>
        [Button]
        public bool TakeHeavyDamage(HealthSystem.HitInfo hitInfo)
        {
            if (_isBreaking) return false;
            if (_healthSystem == null || _healthSystem.IsDead) return false;
            if (hitInfo.heavyDamage <= 0) return false;

            SetBreakPoint(_currentBreakPoint - hitInfo.heavyDamage);

            if (_currentBreakPoint <= 0)
                EnterBreaking(hitInfo);

            return true;
        }

        /// <summary>Restores Break Point to full and exits any Breaking state.</summary>
        public void ResetBreakPointSystem()
        {
            CancelAndDispose(ref _breakingCts);
            _isBreaking = false;
            _lastActualHitDamage = 0;
            SetBreakPoint(maxBreakPoint);
        }

        #endregion

        #region Internals

        /// <summary>Records the last committed ACTUAL (non-heavy) hit for the x N repeat damage.</summary>
        private void HandleOnHit(HealthSystem.HitInfo hitInfo)
        {
            if (hitInfo.heavyDamage > 0) return;
            _lastActualHitDamage = hitInfo.damage;
        }

        private void SetBreakPoint(float value)
        {
            _currentBreakPoint = Mathf.Clamp(value, 0f, maxBreakPoint);
            OnBreakPointChanged?.Invoke(_currentBreakPoint, maxBreakPoint);
        }

        private void EnterBreaking(HealthSystem.HitInfo triggerHit)
        {
            if (_isBreaking) return;
            _isBreaking = true;

            // 1) Cancels all performing skills completely.
            owner?.SkillSystem?.CancelAllSkill();

            // 2) Apply stun (respects Iron Body / invincibility rules inside StunEffect.OnStart).
            ApplyBreakingStun();

            // 3) Take the last actual hit damage x N more (actual damage -> HP, never heavy).
            ApplyBreakingRepeatDamage(triggerHit).Forget();

            // 4) Restore Break Point fully when the stun duration ends.
            _breakingCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            BreakingRecoverLoop(_breakingCts.Token).Forget();

            OnBreakingStart?.Invoke();
        }

        private void ApplyBreakingStun()
        {
            if (!breakingStunEffectData || owner == null) return;

            // Mirror StatusEffectManager.ApplyEffectTo but force our own duration.
            BaseStatusEffect newEffect =
                (BaseStatusEffect)Activator.CreateInstance(breakingStunEffectData.EffectType);
            newEffect.AssignEffectData(breakingStunEffectData, breakingStunDuration);
            owner.StatusEffectSystem.AddEffect(newEffect);
        }

        /// <summary>
        /// Applies the x N repeated actual damage. Retries across frames because the previous
        /// committed hit may still be inside the HealthSystem hit-cooldown window.
        /// </summary>
        private async UniTaskVoid ApplyBreakingRepeatDamage(HealthSystem.HitInfo sourceHit)
        {
            if (_lastActualHitDamage <= 0 || _healthSystem == null) return;

            var repeatHit = new HealthSystem.HitInfo
            {
                attackerId       = sourceHit.attackerId,
                damage           = _lastActualHitDamage * breakingDamageRepeatMultiplier,
                attacker         = sourceHit.attacker,
                realObjectAttack = sourceHit.realObjectAttack
            };

            const int maxRetryFrames = 240; // ~4s @60fps safety bound
            for (int i = 0; i < maxRetryFrames; i++)
            {
                if (this == null || _healthSystem == null) return;

                if (_healthSystem.TakeDamage(repeatHit))
                    return;

                try
                {
                    await UniTask.NextFrame(destroyCancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private async UniTaskVoid BreakingRecoverLoop(CancellationToken token)
        {
            try
            {
                await UniTask.WaitForSeconds(breakingStunDuration, cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                return; // destroyed / reset
            }

            ExitBreaking();
        }

        private void ExitBreaking()
        {
            if (!_isBreaking) return;

            CancelAndDispose(ref _breakingCts);
            _isBreaking = false;
            SetBreakPoint(maxBreakPoint);
            OnBreakingEnd?.Invoke();
        }

        private static void CancelAndDispose(ref CancellationTokenSource cts)
        {
            if (cts == null) return;
            try { cts.Cancel(); } catch { /* ignore */ }
            cts.Dispose();
            cts = null;
        }

        #endregion

        #region Unity Methods

        private void Awake()
        {
            _currentBreakPoint = maxBreakPoint;
        }

        private void OnDestroy()
        {
            if (_healthSystem != null)
                _healthSystem.OnHit -= HandleOnHit;

            CancelAndDispose(ref _breakingCts);
        }

        #endregion
    }
}
