using System.Collections.Generic;
using Characters.HeathSystems;
using Characters.SO.SkillDataSo;
using Feedbacks;
using Manager;
using UnityEngine;

namespace Characters.SkillSystems.SkillObjects
{
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
    /// จรวดนำวิถี: Launch (ไม่มี damage) → Seek (เปิด damage, homing) → ชน: เล่น impact VFX แล้วคืน pool ทันที
    /// </summary>
    public class PhotonBarrageSkillObject : BaseSkillObject
    {
        private enum State { Idle, Launch, Seek }

        [SerializeField] private TrailRenderer trail;
        [SerializeField] private bool rotateToHeading = true;
        [SerializeField] private float headingAngleOffset = 0f;

        [Tooltip("เล่นตอนชนเป้าเท่านั้น (หมดอายุเองจะหายเงียบๆ) ตั้ง FollowMode = Once, StopOnDisable = false")]
        [SerializeField] private VFXPlayerPooling impactVfx;

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
        private int _generation;

        protected override void Awake()
        {
            base.Awake();
            DamageOnTouch.OnHit += HandleHit;
        }

        private void OnEnable()
        {
            if (trail)
            {
                trail.Clear();
                trail.emitting = true;
            }
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
            LayerMask enemyLayer, Vector2 direction, PhotonTargetClaims claims, int generation = 0)
        {
            _data = data;
            _ownerTransform = ownerTransform;
            _ownerId = ownerId;
            _enemyLayer = enemyLayer;
            _claims = claims;
            _generation = generation;

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

            if (!_ownerTransform)
            {
                Finish(false);
                return;
            }

            // ชนในเฟรมก่อนหน้า → จบตอนนี้ (AOE ที่โดนพร้อมกันครบแล้ว)
            if (_hit)
            {
                Finish(true);
                return;
            }

            float dt = Time.fixedDeltaTime;
            _age += dt;

            if (_age >= _data.MaxFlightLifetime)
            {
                Finish(false); // หมดอายุ: หายเงียบๆ
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

            if (_target && !IsTargetValid()) DropTarget();

            if (!_target)
            {
                _retargetTimer -= dt;
                if (_retargetTimer <= 0f)
                {
                    _retargetTimer = _data.RetargetInterval;
                    AcquireTarget();
                }
            }

            _speed = Mathf.MoveTowards(_speed, _data.CruiseSpeed, _data.Acceleration * dt);

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
            // นับเฉพาะตอนเปิด damage แล้ว (Seek) ปิดจริงใน FixedUpdate ถัดไป
            if (_state == State.Seek) _hit = true;
        }
        
        
        private void Finish(bool playImpact)
        {
            if (_state == State.Idle) return;
            _state = State.Idle;

            DropTarget();
            DamageOnTouch.DisableDamage(this);

            // VFX อยู่ใน pool ของมันเอง เล่นจบแล้วคืนเอง ไม่ผูกกับกระสุนที่กำลังจะปิด
            if (playImpact)
            {
                if (impactVfx) impactVfx.PlayVFX();
                TrySpawnChildren(); // งอกเฉพาะตอนชนจริง ไม่งอกตอนหมดอายุ
            }

            gameObject.SetActive(false);
            PoolingManager.Current?.Release(_data.MissilePrefab.name, this);
        }

        private void ApplyRotation()
        {
            if (!rotateToHeading) return;
            float angle = Mathf.Atan2(_heading.y, _heading.x) * Mathf.Rad2Deg + headingAngleOffset;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        
        private void TrySpawnChildren()
        {
            if (_data.SpawnChance <= 0f || _data.SpawnAmount <= 0) return;
            if (_generation >= _data.MaxSpawnGeneration) return;
            if (Random.value * 100f >= _data.SpawnChance) return;
            if (!_ownerTransform) return;

            string poolName = _data.MissilePrefab.name;
            float baseAngle = Mathf.Atan2(_heading.y, _heading.x) * Mathf.Rad2Deg;
            int n = _data.SpawnAmount;
            float spread = _data.SpawnAngleSpread;

            for (int i = 0; i < n; i++)
            {
                // กระจายเป็นพัดรอบทิศเดิม (ถ้า n=1 พุ่งไปทิศเดิม)
                float offset = n > 1 ? Mathf.Lerp(-spread * 0.5f, spread * 0.5f, i / (float)(n - 1)) : 0f;
                float rad = (baseAngle + offset) * Mathf.Deg2Rad;
                Vector2 dir = new(Mathf.Cos(rad), Mathf.Sin(rad));

                var child = PoolingManager.Instance.Get<PhotonBarrageSkillObject>(poolName);
                if (!child) continue;

                child.transform.position = transform.position;
                child.ResetTrail();
                child.gameObject.SetActive(true);
                child.Launch(_data, _ownerTransform, _ownerId, _enemyLayer, dir, _claims, _generation + 1);
            }
        }
    }
}