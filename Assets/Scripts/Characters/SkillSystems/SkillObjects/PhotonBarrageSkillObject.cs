using System.Collections.Generic;
using Characters.HeathSystems;
using Characters.SO.SkillDataSo;
using Manager;
using UnityEngine;

namespace Characters.SkillSystems.SkillObjects
{
    /// <summary>
    /// ตัวจองเป้า ใช้ร่วมกันทุกนัดของ runtime เดียว กันกระสุนหลายนัดพุ่งใส่เป้าเดียวกัน
    /// </summary>
    public class PhotonTargetClaims
    {
        private readonly Dictionary<Transform, int> _claims = new();

        public bool IsClaimed(Transform t) => _claims.TryGetValue(t, out var c) && c > 0;

        public void Claim(Transform t)
        {
            _claims.TryGetValue(t, out var c);
            _claims[t] = c + 1;
        }

        public void Release(Transform t)
        {
            if (!_claims.TryGetValue(t, out var c)) return;
            if (c <= 1) _claims.Remove(t);
            else _claims[t] = c - 1;
        }
    }

    /// <summary>
    /// จรวดนำวิถี: Launch (ไม่มี damage) → Seek (เปิด damage, บินเร่งความเร็วและ homing)
    /// ทำงานด้วยตัวเองทั้งหมด ไม่พึ่ง runtime ที่สร้างมัน
    /// </summary>
    public class PhotonBarrageSkillObject : BaseSkillObject
    {
        private enum State { Idle, Launch, Seek }

        [SerializeField] private TrailRenderer trail;
        [SerializeField] private bool rotateToHeading = true;
        [SerializeField] private float headingAngleOffset = 0f;

        private static readonly Collider2D[] Buffer = new Collider2D[64];

        private State _state = State.Idle;
        private SkillPhotonBarrageDataSo _data;
        private Transform _ownerTransform;
        private string _ownerId;
        private LayerMask _enemyLayer;
        private PhotonTargetClaims _claims;

        private Vector2 _heading;
        private float _speed;
        private float _age;
        private float _seekAge;
        private float _retargetTimer;
        private float _arcSign = 1f;
        private bool _hit;
        private HealthSystem _target;

        protected override void Awake()
        {
            base.Awake();
            DamageOnTouch.OnHit += HandleHit;
        }

        private void OnEnable()
        {
            if (trail) trail.Clear();
        }

        private void OnDisable()
        {
            DropTarget();
            _state = State.Idle;
        }

        public void ResetTrail()
        {
            if (trail) trail.Clear();
        }

        /// <summary>เรียกหลัง SetActive(true)</summary>
        public void Launch(SkillPhotonBarrageDataSo data, Transform ownerTransform, string ownerId,
            LayerMask enemyLayer, Vector2 direction, PhotonTargetClaims claims)
        {
            _data = data;
            _ownerTransform = ownerTransform;
            _ownerId = ownerId;
            _enemyLayer = enemyLayer;
            _claims = claims;

            float jitter = Random.Range(-data.LaunchAngleJitter, data.LaunchAngleJitter);
            _heading = (Quaternion.Euler(0, 0, jitter) * direction).normalized;
            _speed = data.LaunchSpeed;
            _age = 0f;
            _seekAge = 0f;
            _retargetTimer = 0f;
            _arcSign = Random.value < 0.5f ? -1f : 1f;
            _hit = false;
            _target = null;
            _state = State.Launch;
            ApplyRotation();
        }

        private void FixedUpdate()
        {
            if (_state == State.Idle) return;

            if (!_ownerTransform || _hit)
            {
                Finish();
                return;
            }

            float dt = Time.fixedDeltaTime;
            _age += dt;

            if (_age >= _data.MaxFlightLifetime)
            {
                Finish();
                return;
            }

            if (_state == State.Launch) TickLaunch(dt);
            else TickSeek(dt);
        }

        // ───────── Launch ─────────
        private void TickLaunch(float dt)
        {
            float t = Mathf.Clamp01(_age / Mathf.Max(0.01f, _data.LaunchDuration));
            _speed = _data.LaunchSpeed * _data.LaunchSpeedCurve.Evaluate(t);
            transform.position += (Vector3)(_heading * (_speed * dt));

            if (t >= 1f) BeginSeek();
        }

        private void BeginSeek()
        {
            _state = State.Seek;
            _seekAge = 0f;
            _retargetTimer = 0f;

            // เปิด damage ตอนนี้เท่านั้น
            DamageOnTouch.EnableDamage(
                _ownerTransform.gameObject, _ownerId, this,
                _data.DamageHitPerSec, _data.BaseDamagePerHit, _data.DamageMultiplier);
        }

        // ───────── Seek ─────────
        private void TickSeek(float dt)
        {
            _seekAge += dt;
            float seekLife = Mathf.Max(0.01f, _data.MaxFlightLifetime - _data.LaunchDuration);
            float seekT = Mathf.Clamp01(_seekAge / seekLife);

            // ตรวจเป้าเดิม
            if (_target && !IsTargetValid()) DropTarget();

            // ไม่มีเป้า → สแกนเป็นช่วงๆ
            if (!_target)
            {
                _retargetTimer -= dt;
                if (_retargetTimer <= 0f)
                {
                    _retargetTimer = _data.RetargetInterval;
                    AcquireTarget();
                }
            }

            // เร่งแบบจรวด
            _speed = Mathf.MoveTowards(_speed, _data.CruiseSpeed, _data.Acceleration * dt);

            // เลี้ยวเข้าหาเป้า
            if (_target)
            {
                Vector2 to = (Vector2)_target.transform.position - (Vector2)transform.position;
                if (to.sqrMagnitude > 0.0001f)
                {
                    float desired = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
                    float current = Mathf.Atan2(_heading.y, _heading.x) * Mathf.Rad2Deg;
                    float maxTurn = _data.TurnSpeed * _data.TurnRampCurve.Evaluate(seekT) * dt;
                    float next = Mathf.MoveTowardsAngle(current, desired, maxTurn) * Mathf.Deg2Rad;
                    _heading = new Vector2(Mathf.Cos(next), Mathf.Sin(next));
                }
            }

            // Arc swoop
            float arc = _data.ArcStrength * _data.ArcDecayCurve.Evaluate(seekT) * _arcSign;
            Vector2 velocity = _heading * _speed + Vector2.Perpendicular(_heading) * arc;
            transform.position += (Vector3)(velocity * dt);
            ApplyRotation();
        }

        private bool IsTargetValid()
        {
            if (!_target || !_target.gameObject.activeInHierarchy) return false;
            if (!_target.CanAim) return false;

            float r = _data.DetectionRadius;
            return ((Vector2)_target.transform.position - (Vector2)transform.position).sqrMagnitude <= r * r;
        }

        private void AcquireTarget()
        {
            Vector2 origin = transform.position;
            int found = Physics2D.OverlapCircleNonAlloc(origin, _data.DetectionRadius, Buffer, _enemyLayer);
            if (found <= 0) return;

            HealthSystem bestFree = null, bestAny = null;
            float freeSqr = float.MaxValue, anySqr = float.MaxValue;

            for (int i = 0; i < found; i++)
            {
                var col = Buffer[i];
                if (!col) continue;
                if (!CombatManager.TryGetCharacterFromCache(col.gameObject, out var c)) continue;

                var h = c.HealthSystem;
                if (h == null || !h.CanAim) continue;
                if (h.transform == _ownerTransform) continue;

                float sqr = ((Vector2)h.transform.position - origin).sqrMagnitude;
                if (sqr < anySqr) { anySqr = sqr; bestAny = h; }
                if (sqr < freeSqr && !_claims.IsClaimed(h.transform)) { freeSqr = sqr; bestFree = h; }
            }

            HealthSystem pick = bestFree != null ? bestFree : (_data.AllowSharedTargets ? bestAny : null);
            if (pick == null) return;

            _target = pick;
            _claims.Claim(pick.transform);

            Vector2 to = ((Vector2)pick.transform.position - origin).normalized;
            float cross = _heading.x * to.y - _heading.y * to.x;
            _arcSign = cross >= 0f ? 1f : -1f;
        }

        private void DropTarget()
        {
            if (_target == null) return;
            if (_claims != null) _claims.Release(_target.transform);
            _target = null;
        }

        // ───────── Hit / Finish ─────────
        private void HandleHit(GameObject _)
        {
            // นับเฉพาะตอนเปิด damage แล้ว; ปิดใน FixedUpdate ถัดไปเพื่อให้ AOE โดนครบ
            if (_state == State.Seek) _hit = true;
        }

        private void Finish()
        {
            _state = State.Idle;
            DropTarget();
            DamageOnTouch.DisableDamage(this);
            gameObject.SetActive(false);
            PoolingManager.Current?.Release(_data.MissilePrefab.name, this);
        }

        private void ApplyRotation()
        {
            if (!rotateToHeading) return;
            float angle = Mathf.Atan2(_heading.y, _heading.x) * Mathf.Rad2Deg + headingAngleOffset;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}