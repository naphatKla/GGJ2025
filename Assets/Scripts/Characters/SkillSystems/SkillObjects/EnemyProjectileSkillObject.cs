using System;
using Characters.CombatSystems;
using Characters.Controllers;
using Manager;
using UnityEngine;

namespace Characters.SkillSystems.SkillObjects
{
    public class EnemyProjectileSkillObject : BaseSkillObject, IFixedUpdateable
    {
        [SerializeField] private TrailRenderer trail;

        private Vector2 _direction;
        private float _speed;
        private float _lifetime;
        private float _elapsed;
        private int _hitCount;
        private int _maxHits;
        private bool _releaseOnHit;
        private bool _rotateToDirection;
        private float _rotationOffsetDegrees;
        private bool _isLaunched;

        private bool _canBreakByDash;
        private float _dashBreakRadius;
        private GameObject _breakVfxPrefab;
        private float _breakVfxLifetime;
        private string _breakFeedback;
        private bool _debugLogBreak;

        [SerializeField, Tooltip("Debug: วาดวงรัศมีที่ Dash ชนแล้วแตก")]
        private bool debugDrawBreakRadius;

        public event Action<EnemyProjectileSkillObject> OnReleaseRequested;

        protected override void Awake()
        {
            base.Awake();
            DamageOnTouch.OnHit += HandleHit;
        }

        public void Launch(LaunchPayload payload)
        {
            _direction = payload.Direction.sqrMagnitude > 0.0001f ? payload.Direction.normalized : Vector2.right;
            _speed = Mathf.Max(0f, payload.Speed);
            _lifetime = Mathf.Max(0.05f, payload.Lifetime);
            _maxHits = Mathf.Max(1, payload.MaxHits);
            _releaseOnHit = payload.ReleaseOnHit;
            _rotateToDirection = payload.RotateToDirection;
            _rotationOffsetDegrees = payload.RotationOffsetDegrees;
            _canBreakByDash = payload.CanBreakByDash;
            _dashBreakRadius = Mathf.Max(0f, payload.DashBreakRadius);
            _breakVfxPrefab = payload.BreakVfxPrefab;
            _breakVfxLifetime = payload.BreakVfxLifetime;
            _breakFeedback = payload.BreakFeedback;
            _debugLogBreak = payload.DebugLogBreak;
            _elapsed = 0f;
            _hitCount = 0;
            _isLaunched = true;

            transform.position = payload.Position;
            ApplyRotation();
            ResetTrail();

            // ลงทะเบียนก่อน DamageOnTouch -> เช็ค Dash ชนแตกก่อนทำดาเมจ (ถ้าลำดับใน FixedUpdateManager ยังตามนี้)
            FixedUpdateManager.Instance.Register(this);

            DamageOnTouch.ResetDamageOnTouch();
            DamageOnTouch.EnableDamage(
                payload.Owner,
                payload.OwnerId,
                this,
                payload.HitPerSecond,
                payload.HitShape,
                payload.TargetLayer,
                payload.BoxSize,
                payload.CircleRadius,
                payload.BaseDamage,
                payload.DamageMultiplier,
                canHitWithDamageOnTouch: payload.CanHitWithDamageOnTouch);

            gameObject.SetActive(true);
        }

        public void OnFixedUpdate()
        {
            if (!_isLaunched) return;
            if (TryBreakByDash()) return;

            float dt = Time.fixedDeltaTime;
            _elapsed += dt;

            transform.position += (Vector3)(_direction * (_speed * dt));
            ApplyRotation();

            if (_elapsed >= _lifetime)
                RequestRelease();
        }

        public void ForceRelease()
        {
            RequestRelease();
        }

        /// <summary>Player กำลัง Dash และอยู่ในรัศมี -> กระสุนแตก (ไม่นับเป็น Hit)</summary>
        private bool TryBreakByDash()
        {
            if (!_canBreakByDash || !PlayerDashBreaker.IsActive) return false;

            Vector2 offset = PlayerDashBreaker.Position - (Vector2)transform.position;
            if (offset.sqrMagnitude > _dashBreakRadius * _dashBreakRadius) return false;

            BreakByDash();
            return true;
        }

        private void BreakByDash()
        {
            Vector3 position = transform.position;

            if (_breakVfxPrefab)
                Destroy(Instantiate(_breakVfxPrefab, position, Quaternion.identity), _breakVfxLifetime);

            if (!string.IsNullOrEmpty(_breakFeedback) && PlayerDashBreaker.Player &&
                PlayerDashBreaker.Player.TryGetComponent(out BaseController player))
                player.TryPlayFeedback(_breakFeedback);

            if (_debugLogBreak)
                Debug.Log($"[EnemyProjectile] '{name}' broken by player dash at {position}");

            RequestRelease();
        }

        private void HandleHit(GameObject target)
        {
            if (!_isLaunched) return;

            // ชน Player ระหว่าง Dash (กรณี DamageOnTouch อัปเดตก่อนเรา) -> นับเป็นแตก ไม่ใช่ Hit
            if (_canBreakByDash && PlayerDashBreaker.IsActive && target == PlayerDashBreaker.Player.gameObject)
            {
                BreakByDash();
                return;
            }

            _hitCount++;
            if (_releaseOnHit || _hitCount >= _maxHits)
                RequestRelease();
        }

        private void RequestRelease()
        {
            if (!_isLaunched) return;

            Cleanup();
            OnReleaseRequested?.Invoke(this);
        }

        private void Cleanup()
        {
            _isLaunched = false;
            DamageOnTouch.ResetDamageOnTouch();
            FixedUpdateManager.Current?.Unregister(this);
            ResetTrail();
            gameObject.SetActive(false);
        }

        private void ApplyRotation()
        {
            if (!_rotateToDirection) return;

            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle + _rotationOffsetDegrees);
        }

        private void ResetTrail()
        {
            if (trail)
                trail.Clear();
        }

        private void OnDisable()
        {
            if (!_isLaunched) return;
            Cleanup();
        }

        private void OnDrawGizmos()
        {
            if (!debugDrawBreakRadius || !_isLaunched || !_canBreakByDash) return;
            Gizmos.color = PlayerDashBreaker.IsActive ? Color.yellow : Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _dashBreakRadius);
        }

        public readonly struct LaunchPayload
        {
            public readonly GameObject Owner;
            public readonly string OwnerId;
            public readonly Vector2 Position;
            public readonly Vector2 Direction;
            public readonly float Speed;
            public readonly float Lifetime;
            public readonly DamageOnTouch.OverlapShape HitShape;
            public readonly Vector2 BoxSize;
            public readonly float CircleRadius;
            public readonly LayerMask TargetLayer;
            public readonly float HitPerSecond;
            public readonly float BaseDamage;
            public readonly float DamageMultiplier;
            public readonly int MaxHits;
            public readonly bool ReleaseOnHit;
            public readonly bool RotateToDirection;
            public readonly float RotationOffsetDegrees;
            public readonly bool CanHitWithDamageOnTouch;
            public readonly bool CanBreakByDash;
            public readonly float DashBreakRadius;
            public readonly GameObject BreakVfxPrefab;
            public readonly float BreakVfxLifetime;
            public readonly string BreakFeedback;
            public readonly bool DebugLogBreak;

            public LaunchPayload(
                GameObject owner,
                string ownerId,
                Vector2 position,
                Vector2 direction,
                float speed,
                float lifetime,
                DamageOnTouch.OverlapShape hitShape,
                Vector2 boxSize,
                float circleRadius,
                LayerMask targetLayer,
                float hitPerSecond,
                float baseDamage,
                float damageMultiplier,
                int maxHits,
                bool releaseOnHit,
                bool rotateToDirection,
                float rotationOffsetDegrees,
                bool canHitWithDamageOnTouch,
                bool canBreakByDash = false,
                float dashBreakRadius = 0f,
                GameObject breakVfxPrefab = null,
                float breakVfxLifetime = 1f,
                string breakFeedback = null,
                bool debugLogBreak = false)
            {
                Owner = owner;
                OwnerId = ownerId;
                Position = position;
                Direction = direction;
                Speed = speed;
                Lifetime = lifetime;
                HitShape = hitShape;
                BoxSize = boxSize;
                CircleRadius = circleRadius;
                TargetLayer = targetLayer;
                HitPerSecond = hitPerSecond;
                BaseDamage = baseDamage;
                DamageMultiplier = damageMultiplier;
                MaxHits = maxHits;
                ReleaseOnHit = releaseOnHit;
                RotateToDirection = rotateToDirection;
                RotationOffsetDegrees = rotationOffsetDegrees;
                CanHitWithDamageOnTouch = canHitWithDamageOnTouch;
                CanBreakByDash = canBreakByDash;
                DashBreakRadius = dashBreakRadius;
                BreakVfxPrefab = breakVfxPrefab;
                BreakVfxLifetime = breakVfxLifetime;
                BreakFeedback = breakFeedback;
                DebugLogBreak = debugLogBreak;
            }
        }
    }
}
