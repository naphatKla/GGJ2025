using Characters.CombatSystems;
using Sirenix.OdinInspector;
using UnityEngine;

namespace VFX
{
    /// <summary>
    /// ภาพของ Nova Cutter: ใบมีด (shader GGJ/VFX/EnergyBlade) เด้งขึ้นตอนเริ่มสกิล + หางแสง + ประกายตอนโดนศัตรู
    /// วางไว้ที่ root ของ prefab NovaCutter_LVx (ตัวเดียวกับ DamageOnTouch)
    /// </summary>
    public class NovaCutterVFX : MonoBehaviour
    {
        [Title("Refs")]
        [SerializeField] private Transform blade;
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private ParticleSystem hitSpark;

        [Title("Appear")]
        [Tooltip("เวลาเด้งใบมีดตอนเริ่มสกิล")]
        [SerializeField] private float appearTime = 0.12f;
        [SerializeField] private float appearOvershoot = 1.25f;

        [Title("Shards (ใบมีดเล็กบินตาม)")]
        [SerializeField] private Transform[] shards;
        [Tooltip("ใบมีดเล็กบินเข้ามาจากด้านหลังทีละอัน ห่างกันกี่วินาที")]
        [SerializeField] private float shardStagger = 0.05f;
        [SerializeField] private float shardFlyInTime = 0.18f;
        [Tooltip("ระยะที่บินตามมาจากด้านหลังตอนเริ่ม (หน่วย local)")]
        [SerializeField] private float shardFlyInDistance = 1.6f;
        [Tooltip("ลอยขยับขึ้นลงเบาๆ")]
        [SerializeField] private float shardFloatAmplitude = 0.08f;
        [SerializeField] private float shardFloatSpeed = 6f;

        [Title("Hit Spark")]
        [SerializeField] private int sparksPerHit = 10;
        [Tooltip("กันประกายถี่เกิน (DamageOnTouch ยิง OnHit ทุก tick)")]
        [SerializeField] private float minHitInterval = 0.06f;

        private DamageOnTouch _damage;
        private Vector3 _bladeScale;
        private float _appearT;
        private float _lastHitTime = -1f;

        private Vector3[] _shardPos;
        private Vector3[] _shardScale;
        private float _time;

        private void Awake()
        {
            _damage = GetComponent<DamageOnTouch>();
            if (blade) _bladeScale = blade.localScale;
            if (shards == null) shards = new Transform[0];
            _shardPos = new Vector3[shards.Length];
            _shardScale = new Vector3[shards.Length];
            for (int i = 0; i < shards.Length; i++)
            {
                if (!shards[i]) continue;
                _shardPos[i] = shards[i].localPosition;
                _shardScale[i] = shards[i].localScale;
            }
        }

        private void UpdateShards()
        {
            for (int i = 0; i < shards.Length; i++)
            {
                var s = shards[i];
                if (!s) continue;
                // บินเข้าที่จากด้านหลัง (ตามทิศของใบมีดใหญ่) ทีละอัน
                float k = Mathf.Clamp01((_time - shardStagger * (i + 1)) / Mathf.Max(shardFlyInTime, 1e-3f));
                float ease = 1f - Mathf.Pow(1f - k, 3f);
                Vector3 back = Vector3.down * shardFlyInDistance * (1f - ease);
                // ลอยเบาๆ คนละจังหวะ ให้ดูเหมือนบินตาม
                float bob = Mathf.Sin(_time * shardFloatSpeed + i * 1.7f) * shardFloatAmplitude * ease;
                Vector3 side = s.localRotation * Vector3.right;
                s.localPosition = _shardPos[i] + back + side * bob;
                s.localScale = _shardScale[i] * Mathf.Lerp(0.2f, 1f, ease);
            }
        }

        private void OnEnable()
        {
            if (trail) trail.Clear();           // ไม่ให้มีเส้นลากจากตำแหน่งเก่าตอนเปิดใหม่
            _appearT = 0f;
            _time = 0f;
            if (blade) blade.localScale = _bladeScale * 0.4f;
            if (_shardPos != null) UpdateShards();
            if (_damage) _damage.OnHit += OnHit;
        }

        private void OnDisable()
        {
            if (_damage) _damage.OnHit -= OnHit;
            if (blade) blade.localScale = _bladeScale;
        }

        private void Update()
        {
            _time += Time.deltaTime;
            UpdateShards();
            if (!blade || _appearT >= appearTime) return;
            _appearT += Time.deltaTime;
            float k = Mathf.Clamp01(_appearT / Mathf.Max(appearTime, 1e-3f));
            // 0.4 -> overshoot -> 1
            float s = k < 0.6f
                ? Mathf.Lerp(0.4f, appearOvershoot, k / 0.6f)
                : Mathf.Lerp(appearOvershoot, 1f, (k - 0.6f) / 0.4f);
            blade.localScale = _bladeScale * s;
        }

        private void OnHit(GameObject target)
        {
            if (!hitSpark || !target) return;
            if (Time.time - _lastHitTime < minHitInterval) return;
            _lastHitTime = Time.time;

            Vector3 pos = target.transform.position;
            var col = target.GetComponent<Collider2D>();
            if (col) pos = col.ClosestPoint(blade ? blade.position : transform.position);

            var emit = new ParticleSystem.EmitParams { position = pos, applyShapeToPosition = true };
            hitSpark.Emit(emit, sparksPerHit);
        }
    }
}
