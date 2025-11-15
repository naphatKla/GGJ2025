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
    /// Prevents taking damage during temporary invincibility or hit cooldown period.
    /// </summary>
    public class HealthSystem : MonoBehaviour
    {
        #region Inspectors & Variables

        [SerializeField] private bool blockTakeDamageFeedbackOnFinalHit;
        [SerializeField] private bool changeColorOnIframe;
        
        private BaseController owner;

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
        private float _invincibleTimePerHit;

        /// <summary>Whether the character is currently in hit cooldown state.</summary>
        private bool _isHitCooldown;

        /// <summary>Indicates whether the character is dead.</summary>
        [ShowInInspector, ReadOnly] [ShowIf("@UnityEngine.Application.isPlaying")]
        private bool _isDead;

        public bool IsDead => _isDead;

        /// <summary>Event triggered when the character takes damage.</summary>
        public Action<bool> OnTakeDamage { get; set; }
        
        /// <summary>Event triggered when the character heals.</summary>
        public Action OnHeal { get; set; }

        /// <summary>Event triggered when this character dies.</summary>
        public Action OnDead { get; set; }

        public Action OnDeadAnimationFinish { get; set; }

        /// <summary>Event triggered when this character health change.</summary>
        public Action<float> OnHealthChange { get; set; }

        /// <summary>Event triggered when the invincibility state changes.</summary>
        public Action<bool> OnInvincible { get; set; }

        public int TotalDamageTaken { get; set; }
        public int TotalHeal { get; set; }

        public bool IsInvincible => _isInvincible;
        public float HealthPercentage01 => _currentHealth / _maxHealth;

        #endregion

        #region Properties

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;

        #endregion

        #region Methods

        /// <summary>
        /// Assigns the health data for the character.
        /// This method is typically called by the character controller during initialization.
        /// </summary>
        /// <param name="maxHealth">Maximum health to assign.</param>
        /// <param name="invincibleTimePerHit">Cooldown duration after taking damage.</param>
        public void AssignHealthData(float maxHealth, float invincibleTimePerHit, BaseController owner = null)
        {
            _maxHealth = maxHealth;
            _invincibleTimePerHit = invincibleTimePerHit;
            this.owner = owner;
            ResetHealthSystem();
        }

        /// <summary>
        /// Reduces the character's health by the given damage amount.
        /// Prevents damage if the character is invincible, in cooldown, or already dead.
        /// </summary>
        public bool TakeDamage(float damage, out bool dieThisFrame)
        {
            dieThisFrame = false;
            if (_isDead) return false;
            if (_isInvincible || _isHitCooldown)
            {
                OnTakeDamage?.Invoke(false);
                return false;
            }

            ModifyHealth(-damage);
            TotalDamageTaken += (int)damage;
            OnTakeDamage?.Invoke(true);

            HitCooldownHandler().Forget();

            if (_currentHealth <= 0)
            {
                dieThisFrame = true;
                Dead();
            }

            if (dieThisFrame && blockTakeDamageFeedbackOnFinalHit) return true;
            
            if (Cinemachine2DCameraController.Instance != null &&
                Cinemachine2DCameraController.Instance.IsTransformInView(transform))
            {
                owner?.TryPlayFeedback(FeedbackName.Character.TakeDamage);
            }

            return true;
        }

        public void ForceDead()
        {
            Dead();
            ModifyHealth(-_maxHealth);
            if (blockTakeDamageFeedbackOnFinalHit) return;
            
            if (Cinemachine2DCameraController.Instance != null &&
                Cinemachine2DCameraController.Instance.IsTransformInView(transform))
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
            OnHeal?.Invoke();
            owner?.TryPlayFeedback(FeedbackName.Character.Heal);
        }

        private Tween colorTween;
        private Color? startColor;

        /// <summary>Sets the character's invincibility state.</summary>
        public void SetInvincible(bool value)
        {
            _isInvincible = value;
            OnInvincible?.Invoke(_isInvincible);

            // ป้องกัน NRE หาก owner หรือ Body ไม่มี
            if (owner?.Body == null) return;

            if (changeColorOnIframe)
            {
                startColor ??= owner.Body.color;

                colorTween?.Kill();
                var target = value ? Color.cyan : startColor.Value;

                colorTween = owner.Body
                    .DOColor(target, 0.05f)
                    .SetLink(owner.Body.gameObject, LinkBehaviour.KillOnDestroy); // ผูก lifecycle
            }
            
            if (value) owner?.TryPlayFeedback(FeedbackName.Character.Iframe);
            else owner?.TryStopFeedback(FeedbackName.Character.Iframe);
        }

        /// <summary>
        /// Resets the character's health to maximum and revives them if they were dead.
        /// Also clears invincibility and hit cooldown states.
        /// </summary>
        public void ResetHealthSystem()
        {
            _currentHealth = _maxHealth;
            SetInvincible(false);
            _isHitCooldown = false;
            _isDead = false;

            // ยกเลิกงานรอ-dead ค้างทั้งหมด
            CancelAndDispose(ref _linkedDeadCts);
            CancelAndDispose(ref _deadCts);
            
            PlaySpawnFeedbackSync().Forget();
        }

        /// <summary>Starts the hit cooldown period after the character takes damage.</summary>
        public async UniTaskVoid HitCooldownHandler()
        {
            _isHitCooldown = true;
            try
            {
                await UniTask.WaitForSeconds(_invincibleTimePerHit, cancellationToken: destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // object destroyed → ignore
            }

            if (this) _isHitCooldown = false;
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
            if (Cinemachine2DCameraController.Instance != null &&
                Cinemachine2DCameraController.Instance.IsTransformInView(transform))
            {
                owner?.TryPlayFeedback(FeedbackName.Character.Dead);
            }
            
            try
            {
                if (owner != null && owner.FeedbackSystem != null)
                {
                    // รอจนกว่าจะหยุดเล่นอนิเมชันตาย หรือโดนยกเลิก
                    await UniTask.WaitUntil(
                        () => !owner.FeedbackSystem.IsFeedbackPlaying(FeedbackName.Character.Dead) || !gameObject.activeSelf,
                        cancellationToken: token
                    );
                    // หรือจะกัน soft-lock:
                    // await UniTask.WhenAny(
                    //     UniTask.WaitUntil(() => !owner.FeedbackSystem.IsFeedbackPlaying(FeedbackName.Character.Dead), token),
                    //     UniTask.Delay(TimeSpan.FromSeconds(2.0), cancellationToken: token)
                    // );
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
            await UniTask.WaitUntil(() => gameObject.activeSelf).TimeoutWithoutException(TimeSpan.FromSeconds(3f));
            owner?.TryPlayFeedback(FeedbackName.Character.Spawn);
            
        }
        // -------- CTS utilities & cleanup --------
        private static void CancelAndDispose(ref CancellationTokenSource cts)
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

            colorTween?.Kill();
        }

        #endregion
    }
}