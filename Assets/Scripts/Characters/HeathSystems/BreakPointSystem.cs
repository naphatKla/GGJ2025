using System;
using System.Collections.Generic;
using System.Threading;
using Characters.Controllers;
using Characters.FeedbackSystems;
using Characters.SO.SkillDataSo;
using Characters.SO.StatusEffectSO;
using Characters.StatusEffectSystems;
using Characters.StatusEffectSystems.StatusEffects;
using Cysharp.Threading.Tasks;
using Manager;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Characters.HeathSystems
{
    /// <summary>
    /// Break Point system (Bright2 spec — Design Ver 0.0.20).
    /// <para/>
    /// Heavy damage is a value carried alongside a hit's normal damage: the normal damage still goes to
    /// HP, while the heavy portion drains Break Point on top of it (routing handled by <see cref="CombatManager"/>).
    /// When Break Point reaches 0 the owner enters the <b>Breaking</b> state:
    /// <list type="bullet">
    /// <item>Cancels all performing skills completely.</item>
    /// <item>Stuns the owner (default 7s) — respects Iron Body like normal stun.</item>
    /// <item>The attack that broke it deals x N its actual damage (default x10).</item>
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
        [LabelText("Breaking Damage Multiplier")]
        [PropertyTooltip("The attack that breaks the Break Point deals its actual damage multiplied by this "
                         + "value (spec: x10) - it is amplified in place, not dealt again on top.")]
        [FormerlySerializedAs("breakingDamageRepeatMultiplier")]
        private int breakingDamageMultiplier = 10;

        [Title("Dependents")]
        [PropertyTooltip("Stun effect data applied when entering the Breaking state.")]
        [SerializeField] private StunEffectDataSo breakingStunEffectData;

        [Title("Breaking Status Effects")]
        [PropertyTooltip("Applied to the owner the moment it Breaks and REMOVED again when Breaking ends - "
                         + "use it for states that should last exactly as long as the stun (e.g. a "
                         + "vulnerability debuff so the x N window hurts even more). Set a large Override "
                         + "Duration on each payload so nothing expires early.")]
        [LabelText("While Breaking")]
        [SerializeField] private List<StatusEffectDataPayload> effectsWhileBreaking = new();

        [PropertyTooltip("Applied to the owner when Breaking ENDS and then left to run on its own duration - "
                         + "use it for a lingering state after recovery (e.g. a brief speed buff as it "
                         + "shakes off the stun).")]
        [LabelText("After Breaking")]
        [SerializeField] private List<StatusEffectDataPayload> effectsAfterBreaking = new();

        [Title("Skill Cooldowns After Breaking")]
        [Unit(Units.Second), MinValue(0f)]
        [LabelText("All Skills At Least")]
        [PropertyTooltip("When Breaking ends, every skill the owner currently has is put on AT LEAST this many "
                         + "seconds of cooldown. Skills that still have a longer cooldown running keep it. "
                         + "Without this, anything that finished cooling down during the stun fires the "
                         + "instant the boss recovers - a full burst as a reward for being broken. 0 = off.")]
        [SerializeField] private float allSkillsCooldownAfterBreaking;

        [PropertyTooltip("Exact per-skill cooldowns applied when Breaking ends - these win over the value "
                         + "above. Set a skill to 0 to hand it back READY (e.g. a defensive skill the boss "
                         + "should be allowed to answer with).")]
        [LabelText("Per Skill")]
        [ListDrawerSettings(ShowPaging = false)]
        [SerializeField] private List<SkillCooldownRule> skillCooldownsAfterBreaking = new();

        /// <summary>One skill and the cooldown it is set to when Breaking ends.</summary>
        [Serializable]
        public class SkillCooldownRule
        {
            [HorizontalGroup("Row"), HideLabel]
            public BaseSkillDataSo skill;

            [HorizontalGroup("Row", Width = 90), LabelText("Cooldown"), LabelWidth(60), MinValue(0f)]
            public float cooldown;
        }

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
            if (_isBreaking) RemoveEffects(effectsWhileBreaking);

            CancelAndDispose(ref _breakingCts);
            _isBreaking = false;
            _lastActualHitDamage = 0;
            SetBreakPoint(maxBreakPoint);
        }

        #endregion

        #region Internals

        /// <summary>
        /// Records the ACTUAL damage of the last committed hit (fallback base for the x N breaking damage).
        /// Every hit counts - a hit that also carries heavy damage still deals its actual damage to HP.
        /// Hits landed while already Breaking are ignored so the amplified damage can't feed itself.
        /// </summary>
        private void HandleOnHit(HealthSystem.HitInfo hitInfo)
        {
            if (_isBreaking) return;
            if (hitInfo.damage <= 0) return;
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
            ApplyEffects(effectsWhileBreaking);

            // 3) Amplify the hit that broke it to x N (actual damage -> HP, never heavy).
            ApplyBreakingAmplifiedDamage(triggerHit);

            // 4) Restore Break Point fully when the stun duration ends.
            _breakingCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            BreakingRecoverLoop(_breakingCts.Token).Forget();

            OnBreakingStart?.Invoke();
        }

        /// <summary>
        /// Puts the owner's skills on cooldown as it recovers, so Breaking is a punish and not a reset.
        /// The blanket value only ever EXTENDS a cooldown; the per-skill rules set it exactly.
        /// </summary>
        private void ApplyCooldownsAfterBreaking()
        {
            var skillSystem = owner ? owner.SkillSystem : null;
            if (skillSystem == null) return;

            if (allSkillsCooldownAfterBreaking > 0f)
            {
                foreach (var skillData in skillSystem.GetAllCurrentSkillDatas().All)
                {
                    var runtime = skillSystem.GetSkillRuntimeOrDefault(skillData);
                    if (!runtime) continue;

                    runtime.SetCurrentCooldown(Mathf.Max(runtime.CurrentCooldown, allSkillsCooldownAfterBreaking));
                }
            }

            if (skillCooldownsAfterBreaking == null) return;

            foreach (var rule in skillCooldownsAfterBreaking)
            {
                if (rule == null || !rule.skill) continue;

                var runtime = skillSystem.GetSkillRuntimeOrDefault(rule.skill);
                if (!runtime) continue; // not part of the current phase - nothing to set

                runtime.SetCurrentCooldown(rule.cooldown);
            }
        }

        private void ApplyEffects(List<StatusEffectDataPayload> effects)
        {
            if (owner == null || effects == null || effects.Count == 0) return;
            StatusEffectManager.ApplyEffectTo(owner.gameObject, effects);
        }

        private void RemoveEffects(List<StatusEffectDataPayload> effects)
        {
            if (owner == null || effects == null || effects.Count == 0) return;
            StatusEffectManager.RemoveEffectAt(owner.gameObject, effects);
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
        /// Amplifies the hit that broke the Break Point to x N its actual damage.
        /// <para/>
        /// That hit is still sitting in <see cref="HealthSystem"/>'s pending-hit slot at this point
        /// (damage commits only after beforeHitDelay), so it is consumed and re-sent with the
        /// multiplied value - the target takes ONE hit worth x N, not the original hit plus an extra one.
        /// </summary>
        private void ApplyBreakingAmplifiedDamage(HealthSystem.HitInfo triggerHit)
        {
            if (_healthSystem == null) return;

            // Fall back to the last committed hit only when the breaking hit carried no actual damage
            // (e.g. a heavy-only source).
            float baseDamage = triggerHit.damage > 0 ? triggerHit.damage : _lastActualHitDamage;
            if (baseDamage <= 0) return;

            // Free the pending slot so the amplified hit can replace it instead of being rejected.
            _healthSystem.ConsumePendingHit();

            _healthSystem.TakeDamage(new HealthSystem.HitInfo
            {
                attackerId       = triggerHit.attackerId,
                damage           = baseDamage * breakingDamageMultiplier,
                heavyDamage      = 0,
                attacker         = triggerHit.attacker,
                realObjectAttack = triggerHit.realObjectAttack
            });
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
            RemoveEffects(effectsWhileBreaking);
            ApplyEffects(effectsAfterBreaking);
            ApplyCooldownsAfterBreaking();

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
