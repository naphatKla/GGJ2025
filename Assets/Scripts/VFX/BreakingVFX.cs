using System.Collections;
using Cameras;
using Characters.HeathSystems;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VFX
{
    /// <summary>
    /// ภาพตอนบอสเข้าสถานะ Breaking (Break Point = 0) ตามสไลด์ "Break Point" ทั้ง 2 concept:
    /// <list type="bullet">
    /// <item>Concept 2: โล่ปรากฏ แตกร้าวเป็นเส้นซิกแซกจากบนลงล่าง แล้วแยกเป็น 2 ซีกกระเด็นออก</item>
    /// <item>Concept 1: เส้นแตกสีขาวพุ่งออกจากกลางตัวบอส + วงคลื่นกระแทก + เศษแตก + จอสั่น/hit-stop/post-process</item>
    /// <item>ระหว่าง Breaking: รอยร้าวบางๆ ค้างบนตัวบอส, จบ Breaking: โล่สีฟ้าประกอบกลับ (BP เต็ม)</item>
    /// </list>
    /// วางเป็นลูกของบอส (ตัวที่มี <see cref="BreakPointSystem"/>) แล้วจะผูก event เอง
    /// </summary>
    public class BreakingVFX : MonoBehaviour
    {
        public enum Look { Both, SpikesOnly, ShieldOnly }

        [Title("Refs")]
        [Tooltip("ว่างไว้ = หาจากตัวแม่เอง")]
        [SerializeField] private BreakPointSystem breakPoint;
        [Tooltip("เส้นแตกพุ่งออก (concept 1), วงคลื่นกระแทก และรอยร้าวค้างระหว่าง Breaking")]
        [SerializeField] private SpriteRenderer spikes;
        [Tooltip("โล่แตกครึ่ง (concept 2)")]
        [SerializeField] private SpriteRenderer shield;
        [Tooltip("เศษแตกกระเด็น (เล่นครั้งเดียวตอนแตก)")]
        [SerializeField] private ParticleSystem shards;

        [Title("Look")]
        [SerializeField] private Look look = Look.Both;
        [Tooltip("ขนาด (หน่วยโลก) ของ quad โล่ / เส้นแตก")]
        [SerializeField] private float shieldSize = 19f;
        [SerializeField] private float spikeSize = 18f;
        [Tooltip("ความสว่างรวม (คูณทุกชั้น)")]
        [SerializeField, Range(0f, 2f)] private float brightness = 1f;

        [Title("1) Shield Appear + Crack (concept 2)")]
        [SerializeField] private float shieldAppearTime = 0.14f;
        [Tooltip("เวลาที่รอยร้าวไล่จากบนลงล่าง")]
        [SerializeField] private float crackTime = 0.22f;
        [Tooltip("จอสั่นเบาๆ ระหว่างรอยร้าววิ่ง")]
        [SerializeField] private float crackShakeForce = 0.6f;

        [Title("2) Shatter (concept 1)")]
        [SerializeField] private float spikeGrowTime = 0.07f;
        [SerializeField] private float spikeFadeTime = 0.55f;
        [Tooltip("แสงขาวกลางตัวตอนแตก")]
        [SerializeField, Range(0f, 2f)] private float coreFlash = 0.6f;
        [Tooltip("โล่ 2 ซีกแยกออกนานเท่าไร")]
        [SerializeField] private float splitTime = 0.6f;
        [SerializeField, Range(0f, 1f)] private float splitDistance = 0.55f;
        [Tooltip("วงคลื่นกระแทกขยายออกนานเท่าไร")]
        [SerializeField] private float ringTime = 0.45f;
        [SerializeField] private float shatterShakeForce = 3f;
        [SerializeField] private float shatterShakeDuration = 0.4f;
        [SerializeField] private float hitStop = 0.07f;
        [SerializeField, Range(0f, 1f)] private float hitStopTimeScale = 0.05f;

        [Title("2) Shatter Post-process Punch")]
        [SerializeField] private bool postPunch = true;
        [SerializeField] private float punchTime = 0.45f;
        [SerializeField] private float punchBloom = 2.5f;
        [SerializeField] private float punchChromatic = 0.8f;
        [SerializeField] private float punchLensDistortion = -0.25f;

        [Title("3) While Breaking")]
        [Tooltip("รอยร้าวที่ค้างบนตัวบอสระหว่าง Breaking (ความยาว/ความทึบ)")]
        [SerializeField, Range(0f, 1f)] private float lingerLength = 0.5f;
        [SerializeField, Range(0f, 1f)] private float lingerAlpha = 0.45f;
        [SerializeField] private float lingerFlickerSpeed = 3f;
        [Tooltip("ประกายแปลบที่รอยร้าวทุกกี่วินาที (สุ่ม ± ครึ่งหนึ่ง) 0 = ปิด")]
        [SerializeField] private float lingerSparkInterval = 1.2f;

        [Title("4) Recover (Break Point full)")]
        [SerializeField] private float recoverTime = 0.6f;
        [SerializeField, ColorUsage(true, true)] private Color recoverColor = new(0.45f, 1.1f, 1.8f, 1f);
        [SerializeField] private float recoverShakeForce = 1f;

        [Title("Feedbacks (MMF / Feel) - ไม่ใส่ก็ได้")]
        [Tooltip("เล่นตอนโล่แตก (ใส่เสียงแตก / ข้อความ BREAK ฯลฯ ได้)")]
        [SerializeField] private MMF_Player breakFeedback;
        [Tooltip("เล่นตอนจบ Breaking (BP เต็ม)")]
        [SerializeField] private MMF_Player recoverFeedback;

        private static readonly int SpikeAlphaId = Shader.PropertyToID("_SpikeAlpha");
        private static readonly int SpikeLenId = Shader.PropertyToID("_SpikeLen");
        private static readonly int SpikeSeedId = Shader.PropertyToID("_SpikeSeed");
        private static readonly int CoreFlashId = Shader.PropertyToID("_CoreFlash");
        private static readonly int RingRadiusId = Shader.PropertyToID("_RingRadius");
        private static readonly int RingAlphaId = Shader.PropertyToID("_RingAlpha");
        private static readonly int ShieldAlphaId = Shader.PropertyToID("_ShieldAlpha");
        private static readonly int CrackId = Shader.PropertyToID("_Crack");
        private static readonly int SplitId = Shader.PropertyToID("_Split");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        private MaterialPropertyBlock _mpb;
        private Coroutine _routine;
        private Color _baseColor = Color.white;
        private float _seed;

        // รอยร้าวค้าง (Update คุม)
        private bool _lingering;
        private float _lingerT, _nextSpark, _spark;

        private bool _hitStopActive;
        private float _hitStopPrev = 1f;

        private Volume _punch;
        private VolumeProfile _punchProfile;
        private Bloom _bloom;
        private ChromaticAberration _ca;
        private LensDistortion _lens;

        private bool UseSpikes => look != Look.ShieldOnly && spikes;
        private bool UseShield => look != Look.SpikesOnly && shield;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            if (!breakPoint) breakPoint = GetComponentInParent<BreakPointSystem>();
            if (shield && shield.sharedMaterial) _baseColor = shield.sharedMaterial.GetColor(ColorId);
            HideAll();
        }

        private void OnEnable()
        {
            if (!breakPoint) return;
            breakPoint.OnBreakingStart += PlayBreak;
            breakPoint.OnBreakingEnd += PlayRecover;
        }

        private void OnDisable()
        {
            if (breakPoint)
            {
                breakPoint.OnBreakingStart -= PlayBreak;
                breakPoint.OnBreakingEnd -= PlayRecover;
            }
            EndHitStop();
            DestroyPunchVolume();
            _routine = null;
            _lingering = false;
            HideAll();
        }

        private void OnDestroy()
        {
            EndHitStop();
            DestroyPunchVolume();
        }

        // ───────────────────────── API ─────────────────────────

        /// <summary>เข้า Breaking: โล่ร้าว → แตก (เส้นแตก + โล่แยก) แล้วค้างรอยร้าวไว้</summary>
        public void PlayBreak() => Restart(BreakRoutine());

        /// <summary>ออกจาก Breaking: รอยร้าวหาย + โล่สีฟ้าประกอบกลับ</summary>
        public void PlayRecover() => Restart(RecoverRoutine());

        private void Restart(IEnumerator routine)
        {
            if (_routine != null) StopCoroutine(_routine);
            if (!isActiveAndEnabled) return;
            _routine = StartCoroutine(routine);
        }

        // ───────────────────────── Timeline ─────────────────────────

        private IEnumerator BreakRoutine()
        {
            _lingering = false;
            HideAll();
            _seed = Random.Range(0f, 10f);

            // 1) โล่ปรากฏ (หดเข้า + จางเข้า) แล้วร้าวจากบนลงล่าง
            if (UseShield)
            {
                shield.enabled = true;
                for (float t = 0f; t < shieldAppearTime; t += Time.deltaTime)
                {
                    float k = Mathf.Clamp01(t / Mathf.Max(shieldAppearTime, 1e-3f));
                    SetSize(shield, shieldSize * Mathf.LerpUnclamped(1.3f, 1f, EaseOutBack(k)));
                    SetShield(EaseOutCubic(k), 0f, 0f, _baseColor, Mathf.Lerp(1.6f, 1f, k));
                    yield return null;
                }
                SetSize(shield, shieldSize);

                float nextShake = 0f;
                for (float t = 0f; t < crackTime; t += Time.deltaTime)
                {
                    float k = Mathf.Clamp01(t / Mathf.Max(crackTime, 1e-3f));
                    // รอยร้าววิ่งเร็วขึ้นเรื่อยๆ + โล่สั่นเล็กน้อย + สว่างขึ้นก่อนแตก
                    float jitter = Mathf.Sin(t * 90f) * 0.012f * k;
                    SetSize(shield, shieldSize * (1f + jitter));
                    SetShield(1f, EaseInQuad(k), 0f, _baseColor, 1f + 0.4f * k);
                    if (crackShakeForce > 0f && t >= nextShake)
                    {
                        Shake(crackShakeForce * Mathf.Lerp(0.5f, 1f, k), 0.08f);
                        nextShake = t + 0.07f;
                    }
                    yield return null;
                }
                SetShield(1f, 1f, 0f, _baseColor, 1.4f);
            }

            // 2) แตก: เส้นแตกพุ่ง + วงคลื่นกระแทก + โล่แยก 2 ซีก + เศษ + จอสั่น + post-process
            if (hitStop > 0f) StartCoroutine(HitStopRoutine());
            Shake(shatterShakeForce, shatterShakeDuration);
            if (postPunch) StartCoroutine(PunchRoutine());
            if (shards) { shards.Clear(true); shards.Play(true); }
            if (breakFeedback) breakFeedback.PlayFeedbacks(transform.position);
            if (UseSpikes)
            {
                spikes.enabled = true;
                SetSize(spikes, spikeSize);
            }

            float total = Mathf.Max(Mathf.Max(splitTime, ringTime), spikeGrowTime + spikeFadeTime);
            for (float t = 0f; t < total; t += Time.deltaTime)
            {
                if (UseSpikes)
                {
                    float grow = EaseOutCubic(Mathf.Clamp01(t / Mathf.Max(spikeGrowTime, 1e-3f)));
                    float fade = EaseInOutSine(Mathf.Clamp01((t - spikeGrowTime) / Mathf.Max(spikeFadeTime, 1e-3f)));
                    // ยาวเต็ม (เกินนิดหน่อย) แล้วหดลงเหลือรอยร้าวค้าง
                    float len = Mathf.Lerp(grow * 1.05f, lingerLength, fade);
                    float alpha = Mathf.Lerp(1f, lingerAlpha, fade);
                    float rk = Mathf.Clamp01(t / Mathf.Max(ringTime, 1e-3f));
                    SetSpikes(alpha, len, Mathf.Lerp(coreFlash, 0f, fade),
                        Mathf.Lerp(0.15f, 0.95f, EaseOutCubic(rk)), (1f - rk) * (1f - rk));
                }
                if (UseShield)
                {
                    float k = Mathf.Clamp01(t / Mathf.Max(splitTime, 1e-3f));
                    float ease = EaseOutCubic(k);
                    SetSize(shield, shieldSize * Mathf.Lerp(1f, 1.12f, ease));
                    SetShield(1f - EaseInQuad(k), 1f, Mathf.Max(0.002f, ease * splitDistance), _baseColor, Mathf.Lerp(1.6f, 1f, k));
                }
                yield return null;
            }
            if (shield) shield.enabled = false;

            // 3) ค้างรอยร้าวไว้จนกว่า Breaking จบ (Update ทำ flicker + ประกาย)
            if (UseSpikes)
            {
                _lingering = true;
                _lingerT = 0f;
                _spark = 0f;
                _nextSpark = NextSparkDelay();
                SetSpikes(lingerAlpha, lingerLength, 0f, 0f, 0f);
            }
            else if (spikes) spikes.enabled = false;
            _routine = null;
        }

        private IEnumerator RecoverRoutine()
        {
            bool hadCracks = spikes && spikes.enabled;
            float crackAlpha = _lingering ? CurrentLingerAlpha() : lingerAlpha;
            _lingering = false;
            if (recoverFeedback) recoverFeedback.PlayFeedbacks(transform.position);

            if (UseShield)
            {
                shield.enabled = true;
                SetSize(shield, shieldSize * 1.15f);
            }

            const float joinEnd = 0.45f;
            bool joined = false;
            for (float t = 0f; t < recoverTime; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / Mathf.Max(recoverTime, 1e-3f));

                // รอยร้าวหดเข้ากลางแล้วหาย
                if (hadCracks)
                {
                    float c = EaseInQuad(Mathf.Clamp01(k / 0.5f));
                    SetSpikes(crackAlpha * (1f - c), lingerLength * Mathf.Lerp(1f, 0.2f, c), 0f, 0f, 0f);
                }

                if (UseShield)
                {
                    // สองซีกวิ่งเข้าหากัน (ease out back) → แฟลชตอนประกบ → จางหาย
                    float jk = Mathf.Clamp01(k / joinEnd);
                    float join = EaseOutBack(jk);
                    float alpha = k < joinEnd
                        ? EaseOutCubic(jk)
                        : 1f - EaseInQuad((k - joinEnd) / (1f - joinEnd));
                    float flash = k < joinEnd ? 1f : Mathf.Lerp(1.8f, 1f, Mathf.Clamp01((k - joinEnd) / 0.25f));
                    SetSize(shield, shieldSize * Mathf.LerpUnclamped(1.15f, 1f, join));
                    SetShield(alpha, 0f, Mathf.Max(0f, (1f - join) * splitDistance * 0.6f), recoverColor, flash);
                    if (!joined && k >= joinEnd)
                    {
                        joined = true;
                        Shake(recoverShakeForce, 0.2f);
                    }
                }
                yield return null;
            }
            HideAll();
            _routine = null;
        }

        private void Update()
        {
            if (!_lingering || !spikes) return;
            _lingerT += Time.deltaTime;

            // ประกายแปลบเป็นระยะๆ (ไฟฟ้าลัดวงจรในรอยร้าว)
            if (lingerSparkInterval > 0f && _lingerT >= _nextSpark)
            {
                _spark = 1f;
                _nextSpark = _lingerT + NextSparkDelay();
            }
            _spark = Mathf.MoveTowards(_spark, 0f, Time.deltaTime * 4f);

            spikes.GetPropertyBlock(_mpb);
            _mpb.SetFloat(SpikeAlphaId, CurrentLingerAlpha());
            _mpb.SetFloat(SpikeLenId, lingerLength * (1f + 0.08f * _spark));
            _mpb.SetFloat(CoreFlashId, 0.25f * _spark);
            spikes.SetPropertyBlock(_mpb);
        }

        private float CurrentLingerAlpha()
        {
            // flicker แบบนุ่ม (Perlin) + ประกาย
            float n = Mathf.PerlinNoise(_lingerT * lingerFlickerSpeed, _seed);
            return lingerAlpha * (Mathf.Lerp(0.6f, 1f, n) + 0.6f * _spark);
        }

        private float NextSparkDelay() => lingerSparkInterval * Random.Range(0.5f, 1.5f);

        // ───────────────────────── Helpers ─────────────────────────

        private void SetSpikes(float alpha, float len, float flash, float ringRadius, float ringAlpha)
        {
            if (!spikes) return;
            spikes.GetPropertyBlock(_mpb);
            _mpb.SetFloat(SpikeAlphaId, alpha);
            _mpb.SetFloat(SpikeLenId, len);
            _mpb.SetFloat(SpikeSeedId, _seed);
            _mpb.SetFloat(CoreFlashId, flash);
            _mpb.SetFloat(RingRadiusId, ringRadius);
            _mpb.SetFloat(RingAlphaId, ringAlpha);
            _mpb.SetFloat(ShieldAlphaId, 0f);
            _mpb.SetFloat(IntensityId, brightness);
            spikes.SetPropertyBlock(_mpb);
        }

        private void SetShield(float alpha, float crack, float split, Color color, float intensity)
        {
            if (!shield) return;
            shield.GetPropertyBlock(_mpb);
            _mpb.SetFloat(ShieldAlphaId, alpha);
            _mpb.SetFloat(CrackId, crack);
            _mpb.SetFloat(SplitId, split);
            _mpb.SetFloat(SpikeAlphaId, 0f);
            _mpb.SetFloat(RingAlphaId, 0f);
            _mpb.SetColor(ColorId, color);
            _mpb.SetFloat(IntensityId, intensity * brightness);
            shield.SetPropertyBlock(_mpb);
        }

        private static void SetSize(SpriteRenderer sr, float size)
        {
            // GGJ_WhiteSquare = 1 หน่วยโลกต่อ scale 1
            sr.transform.localScale = new Vector3(size, size, 1f);
        }

        private void HideAll()
        {
            if (spikes) spikes.enabled = false;
            if (shield) shield.enabled = false;
        }

        private static float EaseOutCubic(float k) => 1f - Mathf.Pow(1f - k, 3f);
        private static float EaseInQuad(float k) => k * k;
        private static float EaseInOutSine(float k) => -(Mathf.Cos(Mathf.PI * k) - 1f) * 0.5f;
        private static float EaseOutBack(float k)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(k - 1f, 3f) + c1 * Mathf.Pow(k - 1f, 2f);
        }

        // ───────────────────────── Hit-stop / Shake / Post-process ─────────────────────────

        private IEnumerator HitStopRoutine()
        {
            float prev = Time.timeScale;
            if (prev <= hitStopTimeScale || _hitStopActive) yield break; // เกมหยุดอยู่แล้ว (pause / hit-stop อื่น) ไม่ยุ่ง
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

        private static void Shake(float force, float duration)
        {
            if (force <= 0f) return;
            var cam = Cinemachine2DCameraController.Instance;
            if (cam) cam.ShakeCamera(force, null, duration);
        }

        private IEnumerator PunchRoutine()
        {
            EnsurePunchVolume();
            for (float t = 0f; t < punchTime; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Clamp01(t / Mathf.Max(punchTime, 1e-3f));
                float w = 1f - EaseOutCubic(k);
                _bloom.intensity.value = punchBloom * w;
                _ca.intensity.value = punchChromatic * w;
                _lens.intensity.value = punchLensDistortion * Mathf.Sin(k * Mathf.PI) * (1f - k * 0.5f);
                yield return null;
            }
            DestroyPunchVolume();
        }

        private void EnsurePunchVolume()
        {
            if (_punch) return;
            var go = new GameObject("Breaking_Punch");
            _punch = go.AddComponent<Volume>();
            _punch.isGlobal = true;
            _punch.priority = 90f;
            _punch.weight = 1f;
            _punchProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            _bloom = _punchProfile.Add<Bloom>(true);
            _bloom.intensity.Override(0f);
            _ca = _punchProfile.Add<ChromaticAberration>(true);
            _ca.intensity.Override(0f);
            _lens = _punchProfile.Add<LensDistortion>(true);
            _lens.intensity.Override(0f);
            _punch.sharedProfile = _punchProfile;
        }

        private void DestroyPunchVolume()
        {
            if (_punch) Destroy(_punch.gameObject);
            if (_punchProfile) Destroy(_punchProfile);
            _punch = null;
            _punchProfile = null;
        }

#if UNITY_EDITOR
        [Title("Preview (Play Mode)")]
        [Button] private void PreviewBreak() => PlayBreak();
        [Button] private void PreviewRecover() => PlayRecover();
#endif
    }
}
