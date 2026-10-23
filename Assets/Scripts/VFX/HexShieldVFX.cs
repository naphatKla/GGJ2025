using Sirenix.OdinInspector;
using UnityEngine;

namespace VFX
{
    /// <summary>
    /// โล่รังผึ้งของ Parry (shader GGJ/VFX/HexShield) - 3 จังหวะ:
    /// Guard: โล่เล็กรอบตัวระหว่างช่วง Parry, Burst: Parry สำเร็จ -> ขยายถึงรัศมีระเบิดพร้อมคลื่นวิ่งออก แล้วสลายทีละช่อง,
    /// Hide: Parry ไม่โดน -> จางหายเร็วๆ
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class HexShieldVFX : MonoBehaviour
    {
        [Title("Guard (ระหว่างช่วง Parry)")]
        [Tooltip("รัศมีโล่ตอนตั้งการ์ด (หน่วยโลก)")]
        [SerializeField] private float guardRadius = 2.3f;
        [SerializeField] private float guardFadeIn = 0.08f;
        [SerializeField, Range(0f, 1f)] private float guardAlpha = 0.85f;
        [SerializeField] private float guardPulseSpeed = 14f;
        [SerializeField, Range(0f, 0.3f)] private float guardPulseAmount = 0.05f;

        [Title("Burst (Parry สำเร็จ)")]
        [Tooltip("เวลาขยายจากโล่เล็ก -> รัศมีระเบิด")]
        [SerializeField] private float burstExpandTime = 0.22f;
        [Tooltip("เวลาสลาย/จางหาย (เริ่มหลังถึงรัศมีระเบิด ระหว่างนี้ยังขยายต่อ)")]
        [SerializeField] private float burstDissolveTime = 0.6f;
        [Tooltip("ขยายเลยรัศมีระเบิดไปได้สูงสุดกี่เท่า (ขยายต่อเนื่องไม่หยุดแข็ง แต่ช้าลงเรื่อยๆ)")]
        [SerializeField] private float burstOvershoot = 1.3f;
        [SerializeField] private Color normalTint = Color.white;
        [SerializeField] private Color perfectTint = new Color(1f, 0.95f, 0.6f, 1f);
        [Tooltip("ใช้เวลาจริง (ไม่ช้าลงตาม Time Scale ตอน Parry สำเร็จ)")]
        [SerializeField] private bool useUnscaledTime;

        [Title("Hide (Parry ไม่โดน)")]
        [SerializeField] private float hideTime = 0.12f;

        private enum Phase { Off, Guard, Burst, Hide }

        private static readonly int WaveId = Shader.PropertyToID("_Wave");
        private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        private SpriteRenderer _sr;
        private MaterialPropertyBlock _mpb;
        private Phase _phase = Phase.Off;
        private float _t;
        private float _burstRadius;
        private float _fromRadius;
        private float _fromAlpha;
        private Color _tint = Color.white;

        private float Dt => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _mpb = new MaterialPropertyBlock();
            Apply(0f, 0f, -0.2f, 0f, 1f);
            _sr.enabled = false;
        }

        /// <summary>ตั้งการ์ด: โล่เล็กรอบตัว อยู่จนกว่าจะ Burst / Hide</summary>
        public void ShowGuard()
        {
            _phase = Phase.Guard;
            _t = 0f;
            _tint = normalTint;
            _sr.enabled = true;
        }

        /// <summary>Parry สำเร็จ: ขยายถึง radius (หน่วยโลก = รัศมีระเบิดจริง) แล้วสลาย</summary>
        public void Burst(float radius, bool perfect)
        {
            _fromRadius = _phase == Phase.Guard ? CurrentRadius() : guardRadius;
            _burstRadius = Mathf.Max(radius, guardRadius);
            _tint = perfect ? perfectTint : normalTint;
            _phase = Phase.Burst;
            _t = 0f;
            _sr.enabled = true;
        }

        /// <summary>Parry ไม่โดน: จางหาย</summary>
        public void Hide()
        {
            if (_phase != Phase.Guard) return;
            _fromAlpha = _sr.color.a;
            _phase = Phase.Hide;
            _t = 0f;
        }

        private float CurrentRadius() => transform.localScale.x * 0.5f;

        private void Update()
        {
            if (_phase == Phase.Off) return;
            _t += Dt;

            switch (_phase)
            {
                case Phase.Guard:
                {
                    float a = guardAlpha * Mathf.Clamp01(_t / Mathf.Max(guardFadeIn, 1e-3f));
                    float pulse = 1f + Mathf.Sin(_t * guardPulseSpeed) * guardPulseAmount;
                    // คลื่นวิ่งจากกลางออกขอบวนไปเรื่อยๆ ให้ดูมีพลังงาน
                    float wave = Mathf.Repeat(_t * 1.6f, 1.2f) - 0.1f;
                    Apply(guardRadius * pulse, a, wave, 0f, 1f);
                    break;
                }
                case Phase.Burst:
                {
                    float total = burstExpandTime + burstDissolveTime;
                    float u = _t / Mathf.Max(burstExpandTime, 1e-3f);
                    // โค้งขยายต่อเนื่อง: ถึงรัศมีระเบิดพอดีที่ u = 1 แล้วขยายต่อช้าลงเรื่อยๆ จนเข้าใกล้ burstOvershoot (ไม่หยุดแข็ง)
                    float k = Mathf.Log(Mathf.Max(burstOvershoot, 1.01f) / (Mathf.Max(burstOvershoot, 1.01f) - 1f));
                    float grow = (1f - Mathf.Exp(-k * u)) / (1f - Mathf.Exp(-k));
                    float radius = Mathf.LerpUnclamped(_fromRadius, _burstRadius, grow);
                    float e = Mathf.Clamp01(u);
                    float d = Mathf.Clamp01((_t - burstExpandTime) / Mathf.Max(burstDissolveTime, 1e-3f));
                    float alpha = 1f - d * d;
                    float intensity = Mathf.Lerp(2.2f, 1f, e);   // flash ตอนเริ่มระเบิด
                    Apply(radius, alpha, Mathf.Lerp(0f, 1.05f, Mathf.Min(grow, 1f)), d, intensity);
                    if (_t >= total) Stop();
                    break;
                }
                case Phase.Hide:
                {
                    float h = Mathf.Clamp01(_t / Mathf.Max(hideTime, 1e-3f));
                    Apply(CurrentRadius() * (1f - 0.15f * h), _fromAlpha * (1f - h), -0.2f, h * 0.6f, 1f);
                    if (h >= 1f) Stop();
                    break;
                }
            }
        }

        private void Stop()
        {
            _phase = Phase.Off;
            _sr.enabled = false;
        }

        private void Apply(float radius, float alpha, float wave, float dissolve, float intensity)
        {
            float diameter = Mathf.Max(radius, 0.01f) * 2f;
            transform.localScale = new Vector3(diameter, diameter, 1f);
            var c = _tint;
            c.a = alpha;
            _sr.color = c;

            _sr.GetPropertyBlock(_mpb);
            _mpb.SetFloat(WaveId, wave);
            _mpb.SetFloat(DissolveId, dissolve);
            _mpb.SetFloat(IntensityId, intensity);
            _sr.SetPropertyBlock(_mpb);
        }

#if UNITY_EDITOR
        [Title("Preview (Play Mode)")]
        [Button] private void PreviewGuard() => ShowGuard();
        [Button] private void PreviewBurst(float radius = 6f, bool perfect = false) => Burst(radius, perfect);
        [Button] private void PreviewHide() => Hide();
#endif
    }
}
