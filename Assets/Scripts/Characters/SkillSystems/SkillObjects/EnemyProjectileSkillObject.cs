using System;
using Characters.CombatSystems;
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
            _elapsed = 0f;
            _hitCount = 0;
            _isLaunched = true;

            transform.position = payload.Position;
            ApplyRotation();
            ResetTrail();

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
            FixedUpdateManager.Instance.Register(this);
        }

        public void OnFixedUpdate()
        {
            if (!_isLaunched) return;

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

        private void HandleHit(GameObject target)
        {
            if (!_isLaunched) return;

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
                bool canHitWithDamageOnTouch)
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
            }
        }
    }
}
