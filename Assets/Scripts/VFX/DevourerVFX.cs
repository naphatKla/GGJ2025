using System;
using System.Collections;
using Cameras;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VFX
{
    /// <summary>
    /// ภาพของ Devourer (Bright2) - ใช้ VFX ของแมพซ้ำ (Map Event Black Hole / Notify / Repulsion Wave / Galaxy) ปรับขนาดตามรัศมีจริง
    /// Pull: หลุมดำ + ออร่ากาแล็กซีหมุนวน + จอสั่นเบาๆ,
    /// Charge (Phase 3 หยุดดูด): ออร่ายุบเข้าแกน + แกนพลังงานสว่างขึ้นเรื่อยๆ + วงเตือนรัศมีระเบิด,
    /// Explode: คลื่นกระแทกหลายชั้น + แฟลช + Post-process (Bloom/Chromatic/Lens) + จอสั่นแรง
    /// </summary>
    public class DevourerVFX : MonoBehaviour
    {
        [Serializable]
        public class Part
        {
            [Tooltip("Prefab VFX (เช่น BlackHole_L / NotifyBlackhole_L / RepulsionWave)")]
            public GameObject prefab;
            [Tooltip("รัศมีที่ prefab นี้ออกแบบไว้ที่ scale 1 - ใช้คำนวณ scale ให้ตรงรัศมีสกิล")]
            public float referenceRadius = 37f;
            [Tooltip("ปรับขนาดเพิ่ม/ลด หลังคำนวณตามรัศมี")]
            public float sizeMultiplier = 1f;
            [Tooltip("ปรับความเร็วการเล่น (1 = ปกติ)")]
            public float speedMultiplier = 1f;
            [Tooltip("เล่นช้ากว่าจังหวะหลักกี่วินาที (ซ้อนคลื่นหลายชั้น)")]
            public float delay;
        }

        [Title("Pull (ดูด)")]
        [SerializeField] private Part pull = new() { referenceRadius = 37f };
        [Tooltip("ปรับความเร็วหลุมดำให้จบพอดีกับเวลาดูด (prefab ออกแบบไว้ ~4.5 วิ)")]
        [SerializeField] private float pullDesignedDuration = 4.5f;
        [Tooltip("ออร่ากาแล็กซีรอบตัว (วงหลุมดำ + แขนควัน) ขยาย/หมุน ตอนดูด")]
        [SerializeField] private Transform aura;
        [SerializeField] private float auraSpinSpeed = 90f;
        [Tooltip("Dot field (คอนเซปต์หลัก): จุดเรียงเป็นเกลียวถูกดูดเข้าหาตัวบอส ขนาดเท่ารัศมีดูด")]
        [SerializeField] private SpriteRenderer dots;
        [SerializeField, Range(0f, 1f)] private float dotsAlpha = 0.9f;
        [Tooltip("ความเร็วจุดไหลเข้า ตอนเริ่ม -> ตอนจบการดูด (หน่วย = แถวต่อวินาที)")]
        [SerializeField] private Vector2 dotsPullSpeed = new(0.25f, 0.9f);
        [Tooltip("ความเร็วจุดไหลเข้าตอนชาร์จ (ถูกดูดเข้าแกนแรงมาก)")]
        [SerializeField] private float dotsChargeSpeed = 3f;
        [Tooltip("จอสั่นเบาๆ ต่อเนื่องระหว่างดูด (0 = ปิด)")]
        [SerializeField] private float pullShakeForce = 0.25f;
        [SerializeField] private float pullShakeInterval = 0.35f;

        [Title("Charge (Phase 3 หยุดดูด ก่อนระเบิด)")]
        [SerializeField] private Part charge = new() { referenceRadius = 37f };
        [SerializeField] private float chargeDesignedDuration = 1f;
        [Tooltip("แกนพลังงานกลางตัว (วงแหวนเรืองแสง) - หดเล็กลงและสว่างขึ้นเรื่อยๆ จนระเบิด")]
        [SerializeField] private SpriteRenderer core;
        [SerializeField] private float coreStartRadius = 16f;
        [SerializeField] private float coreEndRadius = 4f;
        [SerializeField] private float coreMaxIntensity = 3f;

        [Title("Explode (ระเบิด)")]
        [Tooltip("ซ้อนได้หลายชิ้น เช่น คลื่นกระแทก + แฟลช - ใส่ delay ให้คลื่นวิ่งออกเป็นระลอก")]
        [SerializeField] private Part[] explode = { new() { referenceRadius = 50f } };
        [Tooltip("แฟลชตอนจบของหลุมดำ (End/Star/Glow) เล่นพร้อมระเบิดด้วย")]
        [SerializeField] private bool burstPullEndOnExplode = true;
        [SerializeField] private float explodeShakeForce = 6f;
        [SerializeField] private float explodeShakeDuration = 0.8f;
        [Tooltip("Dot ระเบิดออก: วงจุดพุ่งออกจากแกนไปไกลกว่ารัศมีระเบิด แล้วจางหาย")]
        [SerializeField] private float dotsBlastTime = 0.7f;
        [SerializeField] private float dotsBlastScale = 1.5f;
        [SerializeField] private float dotsBlastSpeed = -6f;
        [Tooltip("หยุดภาพชั่วขณะตอนระเบิด (hit-stop) วินาทีจริง, 0 = ปิด")]
        [SerializeField] private float hitStop = 0.12f;
        [SerializeField, Range(0f, 1f)] private float hitStopTimeScale = 0.05f;

        [Title("Explode Post-process Punch")]
        [SerializeField] private bool postPunch = true;
        [SerializeField] private float punchTime = 0.6f;
        [SerializeField] private float punchBloom = 6f;
        [SerializeField] private float punchChromatic = 1f;
        [SerializeField] private float punchLensDistortion = -0.6f;
        [Tooltip("แฟลชขาวทั้งจอ (Post Exposure) ตอนระเบิด")]
        [SerializeField] private float punchExposure = 2.5f;

        private static readonly int RingRadiusId = Shader.PropertyToID("_RingRadius");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int RingAlphaId = Shader.PropertyToID("_RingAlpha");
        private static readonly int DotAlphaId = Shader.PropertyToID("_DotAlpha");
        private static readonly int DotPhaseId = Shader.PropertyToID("_DotPhase");

        private enum Phase { Off, Pull, Charge, Collapse, Blast }

        private GameObject _pull, _charge;
        private ParticleSystem[] _auraPs;
        private Vector3 _auraScale;
        private MaterialPropertyBlock _mpb;
        private Phase _phase;
        private float _t, _phaseLength, _nextShake;
        private float _dotPhase, _dotRadius;
        private Volume _punch;
        private VolumeProfile _punchProfile;
        private Bloom _bloom;
        private ChromaticAberration _ca;
        private LensDistortion _lens;
        private ColorAdjustments _color;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            if (aura)
            {
                _auraScale = aura.localScale;
                _auraPs = aura.GetComponentsInChildren<ParticleSystem>(true);
                aura.gameObject.SetActive(false);
            }
            if (core) core.enabled = false;
            if (dots) dots.enabled = false;
        }

        // ───────────────────────── API (เรียกจาก SkillDevourerRuntime) ─────────────────────────

        /// <summary>เริ่มดูด: หลุมดำ + ออร่ารอบตัวบอส (ตามตัว) ขนาดตามรัศมีดูด</summary>
        public void PlayPull(float radius, float duration)
        {
            _pull = Spawn(pull, _pull, radius);
            if (_pull)
                Play(_pull, pull.speedMultiplier * Mathf.Max(pullDesignedDuration, 0.1f) / Mathf.Max(duration, 0.1f));

            if (aura)
            {
                aura.gameObject.SetActive(true);
                foreach (var ps in _auraPs) { ps.Clear(true); ps.Play(true); }
            }
            if (dots)
            {
                _dotRadius = radius;
                _dotPhase = 0f;
                dots.enabled = true;
            }
            SetPhase(Phase.Pull, duration);
        }

        /// <summary>หยุดดูด (ครบเวลา): ปล่อยหลุมดำเล่นจนจบเอง</summary>
        public void EndPull(bool clear = false) => Stop(_pull, clear);

        /// <summary>Phase 3: ออร่ายุบเข้าแกน + แกนพลังงานสว่างขึ้น + วงเตือนรัศมีระเบิด</summary>
        public void PlayCharge(float explodeRadius, float duration)
        {
            _charge = Spawn(charge, _charge, explodeRadius);
            if (_charge)
                Play(_charge, charge.speedMultiplier * Mathf.Max(chargeDesignedDuration, 0.1f) / Mathf.Max(duration, 0.1f));
            if (core) core.enabled = true;
            SetPhase(Phase.Charge, duration);
        }

        /// <summary>ระเบิด: คลื่นหลายชั้น + แฟลช + post-process + จอสั่น ณ ตำแหน่งบอสตอนนี้</summary>
        public void PlayExplode(float radius)
        {
            Stop(_charge, true);
            if (burstPullEndOnExplode && _pull) Stop(_pull, true);
            HideAuraAndCore();
            _phase = Phase.Off;
            if (dots && dotsBlastTime > 0f)
            {
                _dotRadius = radius;
                dots.enabled = true;
                SetPhase(Phase.Blast, dotsBlastTime);
            }
            if (hitStop > 0f) StartCoroutine(HitStopRoutine());

            if (explode != null)
                foreach (var part in explode)
                    if (part != null && part.prefab)
                        StartCoroutine(SpawnDetached(part, radius, transform.position));

            Shake(explodeShakeForce, explodeShakeDuration);
            if (postPunch) StartCoroutine(PunchRoutine());
        }

        /// <summary>สกิลจบโดยไม่ระเบิด (Phase 1-2) หรือถูกยกเลิก: ออร่ายุบหาย</summary>
        public void StopAll()
        {
            Stop(_pull, false);
            Stop(_charge, true);
            if (core) core.enabled = false;
            if (_phase != Phase.Off) SetPhase(Phase.Collapse, 0.5f);
        }

        // ───────────────────────── Update ─────────────────────────

        private void SetPhase(Phase p, float length)
        {
            _phase = p;
            _t = 0f;
            _phaseLength = Mathf.Max(length, 0.05f);
            _nextShake = 0f;
        }

        private void Update()
        {
            if (_phase == Phase.Off) return;
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / _phaseLength);

            switch (_phase)
            {
                case Phase.Pull:
                {
                    // ออร่าขยายเข้ามา หมุนเร็วขึ้นเรื่อยๆ
                    float grow = 1f - Mathf.Pow(1f - Mathf.Clamp01(_t / 0.6f), 3f);
                    SetAura(Mathf.Lerp(0.4f, 1f, grow), auraSpinSpeed * Mathf.Lerp(0.6f, 1.6f, k));
                    SetDots(1f, dotsAlpha * grow, Mathf.Lerp(dotsPullSpeed.x, dotsPullSpeed.y, k));
                    if (pullShakeForce > 0f && _t >= _nextShake)
                    {
                        Shake(pullShakeForce * Mathf.Lerp(0.5f, 1.5f, k), pullShakeInterval);
                        _nextShake = _t + pullShakeInterval;
                    }
                    // Phase 1-2 จบแค่นี้ (Phase 3 จะสลับเป็น Charge ทันทีที่ดูดครบ)
                    if (k >= 1f) SetPhase(Phase.Collapse, 0.6f);
                    break;
                }
                case Phase.Charge:
                {
                    // ออร่ายุบเข้าแกน หมุนเร็วมาก + แกนพลังงานหดและสว่างขึ้น (สะสมพลังก่อนระเบิด)
                    float ease = k * k;
                    SetAura(Mathf.Lerp(1f, 0.08f, ease), auraSpinSpeed * Mathf.Lerp(1.6f, 6f, ease));
                    SetDots(Mathf.Lerp(1f, 0.15f, ease), dotsAlpha * Mathf.Lerp(1f, 0.6f, k), dotsChargeSpeed);
                    if (core)
                    {
                        float flicker = 1f + Mathf.Sin(_t * Mathf.Lerp(10f, 60f, k)) * 0.15f * k;
                        SetCore(Mathf.Lerp(coreStartRadius, coreEndRadius, ease),
                            Mathf.Lerp(0.8f, coreMaxIntensity, ease) * flicker, Mathf.Clamp01(_t / 0.15f));
                    }
                    if (_t >= _nextShake)
                    {
                        Shake(Mathf.Lerp(0.3f, 1.2f, k), 0.15f);
                        _nextShake = _t + 0.12f;
                    }
                    break;
                }
                case Phase.Blast:
                {
                    // วงจุดพุ่งออกจากแกน (ไหลออก) แล้วจางหาย
                    float ease = 1f - Mathf.Pow(1f - k, 3f);
                    SetDots(Mathf.Lerp(0.1f, dotsBlastScale, ease), dotsAlpha * (1f - k * k), dotsBlastSpeed * (1f - k));
                    if (k >= 1f) { if (dots) dots.enabled = false; _phase = Phase.Off; }
                    break;
                }
                case Phase.Collapse:
                {
                    SetAura(Mathf.Lerp(1f, 0.001f, k * k), auraSpinSpeed * 2f);
                    SetDots(Mathf.Lerp(1f, 0.05f, k * k), dotsAlpha * (1f - k), dotsChargeSpeed);
                    if (k >= 1f) { HideAuraAndCore(); _phase = Phase.Off; }
                    break;
                }
            }
        }

        private void SetAura(float scale, float spin)
        {
            if (!aura) return;
            aura.localScale = _auraScale * Mathf.Max(scale, 0.001f);
            aura.Rotate(0f, 0f, -spin * Time.deltaTime, Space.Self);
        }

        private void SetDots(float scale, float alpha, float speed)
        {
            if (!dots) return;
            // quad: วงแหวน (ซ่อนไว้) ที่ UV 0.95 = รัศมีดูด
            float d = _dotRadius * 2f / 0.95f * Mathf.Max(scale, 0.001f);
            dots.transform.localScale = new Vector3(d, d, 1f);
            dots.transform.Rotate(0f, 0f, -auraSpinSpeed * 0.35f * Time.deltaTime, Space.Self);
            _dotPhase += speed * Time.deltaTime;
            dots.GetPropertyBlock(_mpb);
            _mpb.SetFloat(DotAlphaId, alpha);
            _mpb.SetFloat(DotPhaseId, _dotPhase);
            _mpb.SetFloat(RingAlphaId, 0f);
            _mpb.SetFloat(RingRadiusId, 0.95f);
            _mpb.SetFloat(IntensityId, 1f);
            dots.SetPropertyBlock(_mpb);
        }

        private void SetCore(float radius, float intensity, float alpha)
        {
            const float ringUv = 0.6f;
            float d = radius * 2f / ringUv;
            core.transform.localScale = new Vector3(d, d, 1f);
            var c = core.color; c.a = alpha; core.color = c;
            core.GetPropertyBlock(_mpb);
            _mpb.SetFloat(RingRadiusId, ringUv);
            _mpb.SetFloat(IntensityId, intensity);
            _mpb.SetFloat(RingAlphaId, 1f);
            core.SetPropertyBlock(_mpb);
        }

        private void HideAuraAndCore()
        {
            if (aura)
            {
                foreach (var ps in _auraPs) if (ps) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                aura.gameObject.SetActive(false);
            }
            if (core) core.enabled = false;
            if (dots) dots.enabled = false;
        }

        // ───────────────────────── Explode helpers ─────────────────────────

        private IEnumerator SpawnDetached(Part part, float radius, Vector3 at)
        {
            if (part.delay > 0f) yield return new WaitForSeconds(part.delay);
            var go = Instantiate(part.prefab, at, Quaternion.identity);
            go.transform.localScale = Vector3.one * Scale(part, radius);
            Play(go, part.speedMultiplier);
            Destroy(go, 6f);
        }

        private IEnumerator PunchRoutine()
        {
            EnsurePunchVolume();
            float t = 0f;
            while (t < punchTime)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / punchTime);
                float w = (1f - k) * (1f - k);
                _punch.weight = 1f;
                _bloom.intensity.value = punchBloom * w;
                _ca.intensity.value = punchChromatic * w;
                _lens.intensity.value = punchLensDistortion * Mathf.Sin(k * Mathf.PI) * (1f - k * 0.5f);
                _color.postExposure.value = punchExposure * Mathf.Pow(1f - Mathf.Clamp01(t / (punchTime * 0.35f)), 2f);
                yield return null;
            }
            DestroyPunchVolume();
        }

        private void EnsurePunchVolume()
        {
            if (_punch) return;
            var go = new GameObject("Devourer_Punch");
            _punch = go.AddComponent<Volume>();
            _punch.isGlobal = true;
            _punch.priority = 90f;
            _punchProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            _bloom = _punchProfile.Add<Bloom>(true);
            _bloom.intensity.Override(0f);
            _ca = _punchProfile.Add<ChromaticAberration>(true);
            _ca.intensity.Override(0f);
            _lens = _punchProfile.Add<LensDistortion>(true);
            _lens.intensity.Override(0f);
            _color = _punchProfile.Add<ColorAdjustments>(true);
            _color.postExposure.Override(0f);
            _punch.sharedProfile = _punchProfile;
        }

        private void DestroyPunchVolume()
        {
            if (_punch) Destroy(_punch.gameObject);
            if (_punchProfile) Destroy(_punchProfile);
            _punch = null;
            _punchProfile = null;
        }

        private bool _hitStopActive;
        private float _hitStopPrev = 1f;

        private IEnumerator HitStopRoutine()
        {
            float prev = Time.timeScale;
            if (prev <= hitStopTimeScale || _hitStopActive) yield break; // เกมหยุดอยู่แล้ว (pause) ไม่ยุ่ง
            _hitStopActive = true;
            _hitStopPrev = prev;
            Time.timeScale = hitStopTimeScale;
            yield return new WaitForSecondsRealtime(hitStop);
            EndHitStop();
        }

        private void EndHitStop()
        {
            if (!_hitStopActive) return;
            _hitStopActive = false;
            if (Mathf.Approximately(Time.timeScale, hitStopTimeScale)) Time.timeScale = _hitStopPrev;
        }

        /// <summary>
        /// ระเบิดเดี่ยวๆ ที่ตำแหน่งหนึ่ง (ไม่มีเจ้าของ) - สร้าง instance ของ prefab นี้, เล่น Explode แล้วทำลายตัวเอง
        /// ใช้กับ Black Hole ที่ถูก Devourer กลืน (ขนาดตามรัศมีระเบิด S/M/L)
        /// </summary>
        public static void SpawnBurst(DevourerVFX prefab, Vector3 position, float radius, float lifetime = 3f)
        {
            if (!prefab) return;
            var vfx = Instantiate(prefab, position, Quaternion.identity);
            vfx.PlayExplode(radius);
            Destroy(vfx.gameObject, lifetime);
        }

        private static void Shake(float force, float duration)
        {
            if (force <= 0f) return;
            var cam = Cinemachine2DCameraController.Instance;
            if (cam) cam.ShakeCamera(force, null, duration);
        }

        // ───────────────────────── Spawning ─────────────────────────

        private GameObject Spawn(Part part, GameObject existing, float radius)
        {
            if (!part.prefab) return null;
            var go = existing ? existing : Instantiate(part.prefab, transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * Scale(part, radius);
            go.SetActive(true);
            return go;
        }

        private static float Scale(Part part, float radius)
            => Mathf.Max(radius, 0.01f) / Mathf.Max(part.referenceRadius, 0.01f) * part.sizeMultiplier;

        private static void Play(GameObject go, float speed)
        {
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.simulationSpeed = speed;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (ps.transform != go.transform && ps.transform.parent.GetComponentInParent<ParticleSystem>()) continue;
                ps.Clear(true);
                ps.Play(true);
            }
        }

        private static void Stop(GameObject go, bool clear)
        {
            if (!go) return;
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
                ps.Stop(false, clear ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
        }

        private void OnDisable()
        {
            EndHitStop(); // ถูกทำลายระหว่าง hit-stop: คืนความเร็วเกม
            DestroyPunchVolume();
            HideAuraAndCore();
            _phase = Phase.Off;
        }

#if UNITY_EDITOR
        [Title("Preview (Play Mode)")]
        [Button] private void PreviewPull(float radius = 50f, float duration = 5f) => PlayPull(radius, duration);
        [Button] private void PreviewCharge(float radius = 50f, float duration = 1.5f) => PlayCharge(radius, duration);
        [Button] private void PreviewExplode(float radius = 50f) => PlayExplode(radius);
#endif
    }
}
