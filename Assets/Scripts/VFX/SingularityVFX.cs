using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VFX
{
    /// <summary>
    /// ภาพของ Singularity (Bright2): วงแหวนขาวขยายถึงรัศมีสกิล + จุด (dot field) ถูกดูดเข้ากลาง,
    /// วงแหวนรอบตัว Bright2 แบบสุริยุปราคา และกล้องเป็นขาวดำระหว่างสกิล
    /// ขาวดำใช้ Volume ของตัวเอง (สร้างตอนเล่น ลบตอนจบ) ไม่ไปแก้ Post-process profile หลักของเกม
    /// </summary>
    public class SingularityVFX : MonoBehaviour
    {
        [Title("Refs")]
        [Tooltip("วงแหวนรัศมีสกิล + dot field")]
        [SerializeField] private SpriteRenderer field;
        [Tooltip("วงแหวนรอบตัว Bright2")]
        [SerializeField] private SpriteRenderer corona;
        [Tooltip("บรรยากาศจักรวาลรอบตัว (Particle จาก Environment_GalacticCore: วงหลุมดำ + แขนกาแล็กซี + ควัน)")]
        [SerializeField] private Transform aura;

        [Title("Aura")]
        [SerializeField] private float auraSpinSpeed = -25f;
        [Tooltip("ตอนจบสกิล ออร่าหดเข้าหาตัวบอส ใช้เวลากี่วินาที")]
        [SerializeField] private float auraCollapseTime = 0.6f;

        [Title("Ring")]
        [Tooltip("ตำแหน่งวงแหวนใน UV ของ quad (shader _RingRadius) - quad ถูกขยายให้วงแหวนตรงกับรัศมีจริง")]
        [SerializeField] private float ringUv = 0.9f;
        [SerializeField] private float expandTime = 0.35f;
        [SerializeField] private float ringPulseSpeed = 3f;
        [SerializeField, Range(0f, 0.2f)] private float ringPulseAmount = 0.04f;

        [Title("Dot Field")]
        [Tooltip("Dot BG อยู่นานกว่าสกิลกี่วินาที (สไลด์: Skill Duration + 1s)")]
        [SerializeField] private float dotLinger = 1f;
        [SerializeField, Range(0f, 1f)] private float dotAlpha = 0.55f;

        [Title("Corona (Bright2)")]
        [Tooltip("รัศมีวงแหวนรอบตัว (หน่วยโลก)")]
        [SerializeField] private float coronaRadius = 3.2f;

        [Title("Black & White Camera")]
        [SerializeField] private bool grayscale = true;
        [SerializeField] private float grayscaleIn = 0.25f;
        [SerializeField] private float grayscaleOut = 0.5f;
        [SerializeField] private float volumePriority = 100f;

        [Title("End")]
        [SerializeField] private float fadeOutTime = 0.4f;

        private static readonly int RingRadiusId = Shader.PropertyToID("_RingRadius");
        private static readonly int DotAlphaId = Shader.PropertyToID("_DotAlpha");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int RingAlphaId = Shader.PropertyToID("_RingAlpha");

        private MaterialPropertyBlock _mpb;
        private float _radius;
        private float _duration;
        private float _t = -1f;
        private float _stopAt = -1f;
        private Volume _volume;
        private VolumeProfile _profile;
        private Vector3 _auraScale;
        private ParticleSystem[] _auraPs;

        private bool Playing => _t >= 0f;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            if (aura)
            {
                _auraScale = aura.localScale;
                _auraPs = aura.GetComponentsInChildren<ParticleSystem>(true);
            }
            SetVisible(false);
        }

        /// <summary>เริ่มสกิล: radius = รัศมีสกิล (หน่วยโลก), duration = ระยะเวลาสกิล</summary>
        public void Play(float radius, float duration)
        {
            _radius = Mathf.Max(radius, 0.1f);
            _duration = Mathf.Max(duration, 0.1f);
            _t = 0f;
            _stopAt = -1f;
            SetVisible(true);
            EnsureVolume();
            if (_auraPs != null)
                foreach (var ps in _auraPs) { ps.Clear(true); ps.Play(true); }
        }

        /// <summary>สกิลจบ/ถูกยกเลิก: เริ่มจาง (dot field อยู่ต่ออีก dotLinger)</summary>
        public void Stop()
        {
            if (!Playing || _stopAt >= 0f) return;
            _stopAt = _t;
        }

        private void Update()
        {
            if (!Playing) return;
            _t += Time.deltaTime;

            // หยุดเองเมื่อครบเวลาสกิล (กันกรณีไม่มีใครเรียก Stop)
            if (_stopAt < 0f && _t >= _duration) _stopAt = _duration;

            float sinceStop = _stopAt >= 0f ? _t - _stopAt : -1f;
            float expand = Mathf.Clamp01(_t / Mathf.Max(expandTime, 1e-3f));
            float easeOut = 1f - Mathf.Pow(1f - expand, 3f);

            // ── ring + dot field ──
            float ringFade = sinceStop < 0f ? 1f : 1f - Mathf.Clamp01(sinceStop / Mathf.Max(fadeOutTime, 1e-3f));
            float dotFade = sinceStop < 0f ? 1f : 1f - Mathf.Clamp01((sinceStop - dotLinger + fadeOutTime) / Mathf.Max(fadeOutTime, 1e-3f));
            float pulse = 1f + Mathf.Sin(_t * ringPulseSpeed * Mathf.PI * 2f) * ringPulseAmount * expand;
            float flash = Mathf.Lerp(2.5f, 1f, expand);

            if (field)
            {
                float diameter = _radius * 2f / Mathf.Max(ringUv, 0.05f);
                field.transform.localScale = new Vector3(diameter, diameter, 1f);
                Apply(field, ringUv * easeOut * pulse, dotAlpha * easeOut * dotFade, flash, ringFade);
            }

            if (corona)
            {
                float k = Mathf.Clamp01(_t / 0.2f) * ringFade;
                float d = coronaRadius * 2f / 0.9f * Mathf.Lerp(1.3f, 1f, easeOut);
                corona.transform.localScale = new Vector3(d, d, 1f);
                var c = corona.color; c.a = k; corona.color = c;
            }

            // ── aura: ขยายเข้ามา, หมุนวน, จบแล้วหดเข้าหาตัวบอส ──
            if (aura)
            {
                float grow = Mathf.Lerp(0.55f, 1f, easeOut);
                float collapse = sinceStop < 0f ? 1f : 1f - Mathf.Clamp01(sinceStop / Mathf.Max(auraCollapseTime, 1e-3f));
                collapse = collapse * collapse;
                aura.localScale = _auraScale * grow * Mathf.Max(collapse, 0.001f);
                aura.Rotate(0f, 0f, auraSpinSpeed * Time.deltaTime, Space.Self);
            }

            // ── black & white ──
            if (_volume)
            {
                float w = sinceStop < 0f
                    ? Mathf.Clamp01(_t / Mathf.Max(grayscaleIn, 1e-3f))
                    : 1f - Mathf.Clamp01(sinceStop / Mathf.Max(grayscaleOut, 1e-3f));
                _volume.weight = w;
            }

            bool allDone = sinceStop >= 0f && sinceStop >= Mathf.Max(Mathf.Max(fadeOutTime, dotLinger), Mathf.Max(grayscaleOut, auraCollapseTime));
            if (allDone) End();
        }

        private void End()
        {
            _t = -1f;
            SetVisible(false);
            DestroyVolume();
        }

        private void Apply(SpriteRenderer sr, float ringRadius, float dots, float intensity, float ringAlpha)
        {
            sr.GetPropertyBlock(_mpb);
            _mpb.SetFloat(RingRadiusId, ringRadius);
            _mpb.SetFloat(DotAlphaId, dots);
            _mpb.SetFloat(IntensityId, intensity);
            _mpb.SetFloat(RingAlphaId, ringAlpha);
            sr.SetPropertyBlock(_mpb);
        }

        private void SetVisible(bool on)
        {
            if (field) field.enabled = on;
            if (corona) corona.enabled = on;
            if (aura) aura.gameObject.SetActive(on);
            if (!on && _auraPs != null)
                foreach (var ps in _auraPs) if (ps) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void EnsureVolume()
        {
            if (!grayscale || _volume) return;
            var go = new GameObject("Singularity_Grayscale");
            _volume = go.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = volumePriority;
            _volume.weight = 0f;
            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var ca = _profile.Add<ColorAdjustments>(true);
            ca.saturation.Override(-100f);
            _volume.sharedProfile = _profile;
        }

        private void DestroyVolume()
        {
            if (_volume) Destroy(_volume.gameObject);
            if (_profile) Destroy(_profile);
            _volume = null;
            _profile = null;
        }

        private void OnDisable()
        {
            if (Playing) End();
        }

        private void OnDestroy() => DestroyVolume();

#if UNITY_EDITOR
        [Title("Preview (Play Mode)")]
        [Button] private void PreviewPlay(float radius = 120f, float duration = 5f) => Play(radius, duration);
        [Button] private void PreviewStop() => Stop();
#endif
    }
}
