using System;
using System.Threading;
using Cameras;
using Characters.Controllers;
using Characters.FeedbackSystems;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.HeathSystems
{
    /// <summary>
    /// Handles the health system of a character, including taking damage, healing,
    /// invincibility status, hit cooldown, and death state.
    /// Provides hit attempt & pending hit buffers for coyote-time style mechanics.
    /// </summary>
    public class HealthSystem : MonoBehaviour
    {
        #region Inspectors & Variables

        [SerializeField] private bool blockTakeDamageFeedbackOnFinalHit;
        [SerializeField] private bool changeColorOnIframe;

        /// <summary>
        /// Delay (in seconds) before a valid hit actually commits damage.
        /// If set to 0 or less, it will still buffer at least 1 frame.
        /// </summary>
        [SerializeField] private float beforeHitDelay = 0f;

        protected BaseController owner;

        /// <summary>The maximum health the character can have.</summary>
        [ShowInInspector, ReadOnly] [ShowIf("@UnityEngine.Application.isPlaying")]
        private float _maxHealth;

        /// <summary>The current health of the character.</summary>
        [ShowInInspector, ReadOnly] [ShowIf("@UnityEngine.Application.isPlaying")]
        private float _currentHealth;

        /// <summary>Determines if the character is temporarily invincible.</summary>
        [ShowInInspector, ReadOnly] [ShowIf("@UnityEngine.Application.isPlaying")]
        private bool _isInvincible;

        /// <summary>
        /// The duration (in seconds) of temporary hit cooldown after taking damage.
        /// During this time, additional hits will be ignored.
        /// </summary>
        protected float invincibleTimePerHit;

        /// <summary>Whether the character is currently in hit cooldown state.</summary>
        private bool _isHitCooldown;

        /// <summary>Indicates whether the character is dead.</summary>
        [ShowInInspector, ReadOnly] [ShowIf("@UnityEngine.Application.isPlaying")]
        private bool _isDead;

        public bool IsDead => _isDead;

        /// <summary>Event triggered when the character takes damage (committed).</summary>
        public Action OnTakeDamage { get; set; }

        /// <summary>Called when damage is actually applied (after delay / pending).</summary>
        public event Action<HitInfo> OnHit;

        /// <summary>Called whenever TakeDamage is requested, regardless of iframe/cooldown/dead.</summary>
        public event Action<HitInfo> OnHitAttempt;

        /// <summary>Called when a valid pending hit starts buffering before committing.</summary>
        public event Action<HitInfo> OnBeforeHit;

        /// <summary>Event triggered when the character heals.</summary>
        public Action<int> OnHeal { get; set; }

        /// <summary>Event triggered when this character dies.</summary>
        public Action OnDead { get; set; }

        public Action OnDeadAnimationFinish { get; set; }

        /// <summary>Event triggered when this character health change.</summary>
        public Action<float> OnHealthChange { get; set; }

        /// <summary>Event triggered when the invincibility state changes.</summary>
        public Action<bool> OnInvincible { get; set; }

        public int TotalDamageTaken { get; set; }
        public int TotalHeal { get; set; }
        public bool CanAim { get; set; } = true;
        public bool IsInvincible => _isInvincible;
        public float HealthPercentage01 => _currentHealth / _maxHealth;

        #endregion

        #region Properties

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;

        /// <summary>Delay before a valid hit is committed. 0 = at least 1 frame buffer.</summary>
        public float BeforeHitDelay
        {
            get => beforeHitDelay;
            set => beforeHitDelay = Mathf.Max(0f, value);
        }

        public struct HitInfo
        {
            public float damage;
            public BaseController attacker;
            public GameObject realObjectAttack;
        }

        #endregion

        #region Buffers & CTS

        /// <summary>ล่าสุดที่ถูก "พยายามตี" ไม่สน iframe/cooldown (อยู่อย่างน้อย 1 frame)</summary>
        private HitInfo? _lastHitAttempt;

        private CancellationTokenSource _hitAttemptCts;

        /// <summary>hit ที่ผ่าน iframe/cooldown แล้ว และกำลังรอ commit จริง</summary>
        private HitInfo? _pendingHit;

        private CancellationTokenSource _pendingHitCts;

        #endregion

        #region Public API

        /// <summary>
        /// Assigns the health data for the character.
        /// This method is typically called by the character controller during initialization.
        /// </summary>
        /// <param name="maxHealth">Maximum health to assign.</param>
        /// <param name="invincibleTimePerHit">Cooldown duration after taking damage.</param>
        public virtual void AssignHealthData(float maxHealth, float invincibleTimePerHit, BaseController owner = null)
        {
            _maxHealth = Mathf.Clamp(maxHealth, 1, 99999999);
            this.invincibleTimePerHit = invincibleTimePerHit;
            this.owner = owner;
            ResetHealthSystem();
        }


        /// <summary>
        /// Core damage entry point.
        /// - Always records a hit attempt (HitAttempt buffer + OnHitAttempt).
        /// - Only creates a pending hit (BeforeHit buffer) if not dead, not iframe, not in hit cooldown.
        /// - Actual damage is committed later via BeforeHitDelay.
        /// Returns true if this hit was accepted as a valid pending hit.
        /// </summary>
        [Button]
        public virtual bool TakeDamage(HitInfo hitInfo)
        {
            // 1) Dead guard
            if (_isDead) return false;

            // 2) Record attempt (even during iframe/cooldown/dead)
            BufferHitAttempt(hitInfo);

            // 3) If iframe or hit cooldown → don't create pending hit
            if (_isInvincible || _isHitCooldown) return false;

            // 4) Create/overwrite pending hit; will commit after delay
            BufferPendingHit(hitInfo);
            return true;
        }
        
        /// <summary>
        /// Consume the last hit attempt (ไม่สน iframe/cooldown).
        /// ใช้สำหรับสกิลที่อยากรู้ว่า เมื่อกี้มี hit อะไรเพิ่งชนเรา
        /// </summary>
        public bool ConsumeHitAttempt(Action<HitInfo> onConsumed = null)
        {
            if (!_lastHitAttempt.HasValue) return false;

            var info = _lastHitAttempt.Value;
            _lastHitAttempt = default;
            CancelAndDispose(ref _hitAttemptCts);
            onConsumed?.Invoke(info);
            return true;
        }

        /// <summary>
        /// Consume the current pending hit (ที่จะโดน commit จริง)
        /// ใช้สำหรับ parry / shield ที่ต้องการ cancel ดาเมจนี้ออกไป
        /// </summary>
        public bool ConsumePendingHit(Action<HitInfo> onConsumed = null)
        {
            if (!_pendingHit.HasValue) return false;

            var info = _pendingHit.Value;
            _pendingHit = default;
            CancelAndDispose(ref _pendingHitCts);
            onConsumed?.Invoke(info);
            return true;
        }

        protected virtual void TakeDamageAction(HitInfo hitInfo)
        {
            ModifyHealth(-hitInfo.damage);
            TotalDamageTaken += (int)hitInfo.damage;
            OnTakeDamage?.Invoke();
        }

        public void ForceDead()
        {
            Dead();
            ModifyHealth(-_maxHealth);
            if (blockTakeDamageFeedbackOnFinalHit) return;

            if (Cinemachine2DCameraController.Current != null &&
                Cinemachine2DCameraController.Current.IsTransformInView(transform))
            {
                owner?.TryPlayFeedback(FeedbackName.Character.TakeDamage);
            }
        }

        /// <summary>Increases the character's health by the given amount, up to the maximum health.</summary>
        public void Heal(float healAmount)
        {
            if (_isDead) return;
            ModifyHealth(healAmount);
            TotalHeal += (int)healAmount;
            OnHeal?.Invoke((int)healAmount);
            owner?.TryPlayFeedback(FeedbackName.Character.Heal);
        }

        /// <summary>Sets the character's invincibility state.</summary>
        public void SetInvincible(bool value)
        {
            _isInvincible = value;
            OnInvincible?.Invoke(_isInvincible);

            // ถ้าเพิ่งเปิด iframe → ยกเลิกดาเมจที่กำลังรอคิวอยู่
            if (value)
            {
                ConsumePendingHit();
                // ไม่จำเป็นต้องยุ่งกับ HitAttempt; มันมีไว้ให้สกิลมาอ่านเอง
            }

            // ป้องกัน NRE หาก owner หรือ Body ไม่มี
            if (owner?.Body == null) return;

            if (changeColorOnIframe)
            {
                _startColor ??= owner.Body.color;

                _colorTween?.Kill();
                var target = value ? Color.cyan : _startColor.Value;

                _colorTween = owner.Body
                    .DOColor(target, 0.05f)
                    .SetLink(owner.Body.gameObject, LinkBehaviour.KillOnDestroy);
            }

            if (value) owner?.TryPlayFeedback(FeedbackName.Character.Iframe);
            else owner?.TryStopFeedback(FeedbackName.Character.Iframe);
        }

        /// <summary>
        /// Resets the character's health to maximum and revives them if they were dead.
        /// Also clears invincibility and hit cooldown states and pending/attempt hits.
        /// </summary>
        public void ResetHealthSystem()
        {
            _currentHealth = _maxHealth;
            SetInvincible(false);
            _isHitCooldown = false;
            _isDead = false;

            // ยกเลิกงานรอ-dead / pending / attempt ค้างทั้งหมด
            CancelAndDispose(ref _linkedDeadCts);
            CancelAndDispose(ref _deadCts);
            CancelAndDispose(ref _pendingHitCts);
            CancelAndDispose(ref _hitAttemptCts);

            _pendingHit = null;
            _lastHitAttempt = null;

            PlaySpawnFeedbackSync().Forget();
        }

        /// <summary>Starts the hit cooldown period after the character takes damage.</summary>
        public async UniTaskVoid HitCooldownHandler()
        {
            _isHitCooldown = true;
            try
            {
                await UniTask.WaitForSeconds(invincibleTimePerHit, cancellationToken: destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // object destroyed → ignore
            }

            if (this) _isHitCooldown = false;
        }

        #endregion

        #region Internals

        private Tween _colorTween;
        private Color? _startColor;

        protected void BufferHitAttempt(HitInfo hitInfo)
        {
            _lastHitAttempt = hitInfo;
            OnHitAttempt?.Invoke(hitInfo);

            // อายุเท่ากับ PendingHit: ใช้ beforeHitDelay เดียวกัน
            CancelAndDispose(ref _hitAttemptCts);
            _hitAttemptCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            ExpireHitAttempt(_hitAttemptCts.Token).Forget();
        }

        private async UniTaskVoid ExpireHitAttempt(CancellationToken token)
        {
            try
            {
                if (beforeHitDelay > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(beforeHitDelay), cancellationToken: token);
                }
                else
                {
                    // อย่างน้อย 1 frame เหมือนเดิม ถ้า delay = 0
                    await UniTask.NextFrame(token);
                }
            }
            catch (OperationCanceledException)
            {
                // ถูก cancel จาก ConsumeHitAttempt / Reset / Destroy
                return;
            }

            _lastHitAttempt = null;
            CancelAndDispose(ref _hitAttemptCts);
        }

        private void BufferPendingHit(HitInfo hitInfo)
        {
            // ถ้ามี pending อยู่แล้ว → ไม่รับ hit ใหม่ในช่วง beforeHitDelay
            if (_pendingHit.HasValue)
                return;

            // เคลียร์ของเก่า (กันกรณีหลงเหลือจาก state เก่า เช่น revive/reset)
            CancelAndDispose(ref _pendingHitCts);

            _pendingHit = hitInfo;
            _pendingHitCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

            OnBeforeHit?.Invoke(hitInfo);

            ResolvePendingHit(hitInfo, _pendingHitCts.Token).Forget();
        }


        private async UniTaskVoid ResolvePendingHit(HitInfo info, CancellationToken token)
        {
            try
            {
                if (beforeHitDelay > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(beforeHitDelay), cancellationToken: token);
                else
                    // อย่างน้อย 1 frame
                    await UniTask.NextFrame(token);
            }
            catch (OperationCanceledException)
            {
                // ถูก cancel จาก ConsumePendingHit / Destroy / Reset
                return;
            }

            // ยังเป็น hit เดิมอยู่ไหม
            if (!_pendingHit.HasValue || !_pendingHit.Value.Equals(info))
                return;

            _pendingHit = null;
            CancelAndDispose(ref _pendingHitCts);

            // ตรงนี้ค่อย commit ดาเมจจริง
            ApplyDamageNow(info);
        }

        private void ApplyDamageNow(HitInfo hitInfo)
        {
            TakeDamageAction(hitInfo);
            HitCooldownHandler().Forget();

            if (_currentHealth <= 0)
            {
                Dead();
                hitInfo.attacker?.CombatSystem.OnKillHandler(owner);
                if (blockTakeDamageFeedbackOnFinalHit) return;
            }

            if (Cinemachine2DCameraController.Current != null &&
                Cinemachine2DCameraController.Current.IsTransformInView(transform))
            {
                owner?.TryPlayFeedback(FeedbackName.Character.TakeDamage);
            }

            // ตอนนี้ค่อยถือว่า "โดนดาเมจจริง" แล้ว
            OnHit?.Invoke(hitInfo);
        }

        /// <summary>Modifies the character's health by a given value.</summary>
        private void ModifyHealth(float value)
        {
            _currentHealth += value;
            _currentHealth = Mathf.Clamp(_currentHealth, 0, _maxHealth);
            OnHealthChange?.Invoke(value);
        }

        /// <summary>Triggers the character's death state if their health reaches zero.</summary>
        private void Dead()
        {
            if (_isDead) return;
            // ยกเลิกงานเก่า แล้วสร้าง cts ใหม่
            CancelAndDispose(ref _linkedDeadCts);
            CancelAndDispose(ref _deadCts);

            _deadCts = new CancellationTokenSource();
            _linkedDeadCts = CancellationTokenSource.CreateLinkedTokenSource(_deadCts.Token, destroyCancellationToken);

            WaitDeadAnim(_linkedDeadCts.Token).Forget();

            _isDead = true;
            OnDead?.Invoke();
        }

        private CancellationTokenSource _deadCts; // ยกเลิกเมื่อ revive/reset
        private CancellationTokenSource _linkedDeadCts; // ลิงก์กับ destroyCancellationToken

        private async UniTaskVoid WaitDeadAnim(CancellationToken token)
        {
            if (Cinemachine2DCameraController.Current != null &&
                Cinemachine2DCameraController.Current.IsTransformInView(transform))
            {
                owner?.TryPlayFeedback(FeedbackName.Character.Dead);
            }

            try
            {
                if (owner != null && owner.FeedbackSystem != null)
                {
                    // รอจนกว่าจะหยุดเล่นอนิเมชันตาย หรือโดนยกเลิก
                    await UniTask.WaitUntil(
                        () => !owner.FeedbackSystem.IsFeedbackPlaying(FeedbackName.Character.Dead) ||
                              !gameObject.activeSelf,
                        cancellationToken: token
                    );
                }
            }
            catch (OperationCanceledException)
            {
                // ถูกยกเลิกจาก Reset/Destroy → ออกเฉย ๆ
                return;
            }

            OnDeadAnimationFinish?.Invoke();
            if (this && gameObject) gameObject.SetActive(false);
        }

        private async UniTaskVoid PlaySpawnFeedbackSync()
        {
            await UniTask.WaitUntil(() => gameObject.activeSelf)
                .TimeoutWithoutException(TimeSpan.FromSeconds(3f));
            owner?.TryPlayFeedback(FeedbackName.Character.Spawn);
        }

        // -------- CTS utilities & cleanup --------
        protected static void CancelAndDispose(ref CancellationTokenSource cts)
        {
            if (cts == null) return;
            try
            {
                cts.Cancel();
            }
            catch
            {
                /* ignore */
            }

            cts.Dispose();
            cts = null;
        }

        private void OnDestroy()
        {
            CancelAndDispose(ref _linkedDeadCts);
            CancelAndDispose(ref _deadCts);
            CancelAndDispose(ref _pendingHitCts);
            CancelAndDispose(ref _hitAttemptCts);

            _colorTween?.Kill();
        }

        #endregion
    }
}