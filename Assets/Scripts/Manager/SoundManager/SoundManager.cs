using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;
using Manager;                  // PoolingManager + PoolingGroupName
using Manager.SoundManager;     // SoundDatabase, SoundName

namespace Manager.SoundManager
{
    /// <summary>
    /// SoundManager: เล่น SFX/UI ด้วย key จาก SoundDatabase (รองรับหลายคลิป+โหมดสุ่ม), และ BGM crossfade
    /// - SFX (3D/2D ตามคอนฟิกของ key)
    /// - UI (2D เสมอ)
    /// - BGM 2 แชนแนลสำหรับ crossfade
    /// - ใช้ AudioSource pooling ผ่าน PoolingManager
    /// </summary>
    public class SoundManager : MMSingleton<SoundManager>
    {
        [TitleGroup("Database"), SerializeField, Required]
        private SoundDatabase database;

        [TitleGroup("Mixer Defaults")]
        [SerializeField] private AudioMixerGroup defaultSfxMixer;
        [SerializeField] private AudioMixerGroup defaultUiMixer;
        [SerializeField] private AudioMixerGroup defaultBgmMixer;

        [TitleGroup("Pools")]
        [SerializeField, MinValue(1)] private int sfxDefaultCapacity = 16;
        [SerializeField, MinValue(1)] private int sfxMaxSize = 64;
        [SerializeField, MinValue(1)] private int uiDefaultCapacity = 16;
        [SerializeField, MinValue(1)] private int uiMaxSize = 64;

        [TitleGroup("BGM")]
        [SerializeField, Range(0f, 5f)] private float defaultFadeIn = 0.6f;
        [SerializeField, Range(0f, 5f)] private float defaultFadeOut = 0.6f;

        // ---------- Pool keys ----------
        private const string POOL_SFX = "SFX_AudioSource";
        private const string POOL_UI  = "UI_AudioSource";

        // ---------- Cooldown ----------
        private readonly Dictionary<string, float> _lastPlayAt = new(StringComparer.Ordinal);

        // ---------- Random state per key ----------
        private class KeyState
        {
            public int lastIndex = -1;
            public List<int> shuffleBag; // ใช้กับ ShuffleNoRepeat
        }
        private readonly Dictionary<string, KeyState> _sfxStates = new(StringComparer.Ordinal);
        private readonly Dictionary<string, KeyState> _uiStates  = new(StringComparer.Ordinal);

        // ---------- BGM ----------
        private AudioSource _bgmA;
        private AudioSource _bgmB;
        private AudioSource _bgmActive;
        private Coroutine _bgmFadeRoutine;

        protected override void Awake()
        {
            base.Awake();

            if (database == null)
            {
                Debug.LogError("[SoundManager] SoundDatabase is not assigned.");
            }
            else
            {
                database.Rebuild();
            }

            EnsureSfxPool();
            EnsureUiPool();
            EnsureBgmChannels();
        }

        #region Pool Setup

        private void EnsureSfxPool()
        {
            PoolingManager.Instance.Create<AudioSource>(
                nameOrID: POOL_SFX,
                groupName: PoolingGroupName.Sound,
                createFunc: () =>
                {
                    var go = new GameObject("SFX_Source");
                    var src = go.AddComponent<AudioSource>();
                    src.playOnAwake = false;
                    src.loop = false;
                    src.spatialBlend = 0f;
                    src.outputAudioMixerGroup = defaultSfxMixer;
                    return src;
                },
                onGet: src => { src.gameObject.SetActive(true); },
                onRelease: src =>
                {
                    if (!src) return;
                    src.Stop();
                    src.clip = null;
                    src.loop = false;
                    src.pitch = 1f;
                    src.spatialBlend = 0f;
                    src.minDistance = 1f;
                    src.maxDistance = 500f;
                    src.rolloffMode = AudioRolloffMode.Logarithmic;
                    src.transform.localPosition = Vector3.zero;
                    src.outputAudioMixerGroup = defaultSfxMixer;
                    src.gameObject.SetActive(false);
                },
                onDestroy: src => { if (src) Destroy(src.gameObject); },
                collectionCheck: false,
                defaultCapacity: sfxDefaultCapacity,
                maxSize: sfxMaxSize
            );
        }

        private void EnsureUiPool()
        {
            PoolingManager.Instance.Create<AudioSource>(
                nameOrID: POOL_UI,
                groupName: PoolingGroupName.Sound,
                createFunc: () =>
                {
                    var go = new GameObject("UI_Source");
                    go.transform.SetParent(transform, false);
                    var src = go.AddComponent<AudioSource>();
                    src.playOnAwake = false;
                    src.loop = false;
                    src.spatialBlend = 0f; // UI เป็น 2D
                    src.outputAudioMixerGroup = defaultUiMixer ? defaultUiMixer : defaultSfxMixer;
                    return src;
                },
                onGet: src => { src.gameObject.SetActive(true); },
                onRelease: src =>
                {
                    if (!src) return;
                    src.Stop();
                    src.clip = null;
                    src.loop = false;
                    src.pitch = 1f;
                    src.spatialBlend = 0f;
                    src.transform.localPosition = Vector3.zero;
                    src.outputAudioMixerGroup = defaultUiMixer ? defaultUiMixer : defaultSfxMixer;
                    src.gameObject.SetActive(false);
                },
                onDestroy: src => { if (src) Destroy(src.gameObject); },
                collectionCheck: false,
                defaultCapacity: uiDefaultCapacity,
                maxSize: uiMaxSize
            );
        }

        #endregion

        #region SFX (Gameplay/World) API

        /// <summary>
        /// เล่น SFX จาก key. หากคีย์มีหลายคลิป จะเลือกตามโหมดสุ่มที่กำหนดใน Database.
        /// สำหรับ 3D ให้ส่ง worldPos.
        /// </summary>
        public AudioSource PlaySFX(string key, Vector3? worldPos = null, float volumeScale = 1f, float? pitchOverride = null)
        {
            if (!TryGetSfx(key, out var entry)) return null;

            // per-key cooldown
            if (IsOnCooldown(key, entry.cooldown)) return null;

            var state = GetOrCreateState(_sfxStates, key);
            int picked = entry.PickIndex(ref state.lastIndex, ref state.shuffleBag);
            if (picked < 0 || picked >= entry.variants.Count) return null;

            var variant = entry.variants[picked];
            if (!variant.clip) return null;

            var src = PoolingManager.Instance.Get<AudioSource>(POOL_SFX);
            if (!src) return null;

            // apply clip & variant config
            ApplyVariantToSource(src, variant, volumeScale, pitchOverride);

            // 3D config จาก entry
            if (entry.spatial)
            {
                src.spatialBlend = Mathf.Clamp01(entry.spatialBlend);
                src.minDistance = Mathf.Max(0.01f, entry.minDistance);
                src.maxDistance = Mathf.Max(src.minDistance + 0.01f, entry.maxDistance);
                src.rolloffMode = entry.rolloffMode;
                if (worldPos.HasValue) src.transform.position = worldPos.Value;
            }
            else
            {
                src.spatialBlend = 0f;
                src.transform.localPosition = Vector3.zero;
            }

            // mixer per-key (ถ้าไม่ตั้งจะ fallback เป็น defaultSfxMixer)
            src.outputAudioMixerGroup = entry.mixerOverride ? entry.mixerOverride : defaultSfxMixer;

            src.Play();

            if (!src.loop)
                StartCoroutine(ReleaseWhenFinished(POOL_SFX, src));

            return src;
        }

        public void StopSFX(AudioSource src, bool release = true)
        {
            if (!src) return;
            src.Stop();
            if (release) PoolingManager.Instance.Release(POOL_SFX, src);
        }

        #endregion

        #region UI (2D) API

        /// <summary>
        /// เล่นเสียง UI จาก key (2D เสมอ). รองรับหลายคลิป/โหมดสุ่ม.
        /// </summary>
        public AudioSource PlayUI(string key, float volumeScale = 1f, float? pitchOverride = null)
        {
            if (!TryGetUi(key, out var entry)) return null;

            if (IsOnCooldown(key, entry.cooldown)) return null;

            var state = GetOrCreateState(_uiStates, key);
            int picked = entry.PickIndex(ref state.lastIndex, ref state.shuffleBag);
            if (picked < 0 || picked >= entry.variants.Count) return null;

            var variant = entry.variants[picked];
            if (!variant.clip) return null;

            var src = PoolingManager.Instance.Get<AudioSource>(POOL_UI);
            if (!src) return null;

            // apply variant (UI เป็น 2D)
            ApplyVariantToSource(src, variant, volumeScale, pitchOverride);
            src.spatialBlend = 0f;
            src.transform.localPosition = Vector3.zero;

            src.outputAudioMixerGroup = entry.mixerOverride ? entry.mixerOverride : (defaultUiMixer ? defaultUiMixer : defaultSfxMixer);

            src.Play();

            if (!src.loop)
                StartCoroutine(ReleaseWhenFinished(POOL_UI, src));

            return src;
        }

        public void StopUI(AudioSource src, bool release = true)
        {
            if (!src) return;
            src.Stop();
            if (release) PoolingManager.Instance.Release(POOL_UI, src);
        }

        #endregion

        #region BGM

        private void EnsureBgmChannels()
        {
            _bgmA = CreateBgmSource("_BGM_A");
            _bgmB = CreateBgmSource("_BGM_B");
            _bgmActive = _bgmA;
        }

        private AudioSource CreateBgmSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 0f;
            src.volume = 0f;
            src.outputAudioMixerGroup = defaultBgmMixer;
            return src;
        }

        public void PlayBGM(string key, float? fadeOut = null, float? fadeIn = null)
        {
            if (!TryGetBgm(key, out var cfg)) return;

            var next = _bgmActive == _bgmA ? _bgmB : _bgmA;
            next.clip = cfg.clip;
            next.outputAudioMixerGroup = cfg.mixerOverride ? cfg.mixerOverride : defaultBgmMixer;
            next.loop = true;
            next.volume = 0f;
            next.Play();

            float fo = fadeOut ?? defaultFadeOut;
            float fi = fadeIn ?? defaultFadeIn;

            if (_bgmFadeRoutine != null) StopCoroutine(_bgmFadeRoutine);
            _bgmFadeRoutine = StartCoroutine(CrossFadeBGM(_bgmActive, next, fo, fi, targetInVolume: Mathf.Clamp01(cfg.volume)));

            _bgmActive = next;
        }

        public void StopBGM(float? fadeOut = null)
        {
            float fo = fadeOut ?? defaultFadeOut;
            if (_bgmActive == null) return;

            if (_bgmFadeRoutine != null) StopCoroutine(_bgmFadeRoutine);
            _bgmFadeRoutine = StartCoroutine(FadeOutThenStop(_bgmActive, fo));
        }

        public void PauseBGM()
        {
            if (_bgmA.isPlaying) _bgmA.Pause();
            if (_bgmB.isPlaying) _bgmB.Pause();
        }

        public void ResumeBGM()
        {
            if (_bgmA.clip) _bgmA.UnPause();
            if (_bgmB.clip) _bgmB.UnPause();
        }

        private IEnumerator CrossFadeBGM(AudioSource from, AudioSource to, float fadeOut, float fadeIn, float targetInVolume)
        {
            float t = 0f;
            float fromStart = from ? from.volume : 0f;
            float toStart = to.volume;

            float total = Mathf.Max(fadeOut, fadeIn);
            if (total <= 0f) total = 0.0001f;

            while (t < total)
            {
                t += Time.unscaledDeltaTime;
                if (from)
                {
                    float k = fadeOut > 0f ? Mathf.Clamp01(t / fadeOut) : 1f;
                    from.volume = Mathf.Lerp(fromStart, 0f, k);
                }
                if (to)
                {
                    float k = fadeIn > 0f ? Mathf.Clamp01(t / fadeIn) : 1f;
                    to.volume = Mathf.Lerp(toStart, targetInVolume, k);
                }
                yield return null;
            }

            if (from)
            {
                from.Stop();
                from.volume = 0f;
                from.clip = null;
            }

            if (to) to.volume = targetInVolume;
            _bgmFadeRoutine = null;
        }

        private IEnumerator FadeOutThenStop(AudioSource src, float fadeOut)
        {
            if (!src) yield break;
            float start = src.volume;
            float t = 0f;
            float total = Mathf.Max(0.0001f, fadeOut);

            while (t < total)
            {
                t += Time.unscaledDeltaTime;
                float k = fadeOut > 0f ? Mathf.Clamp01(t / fadeOut) : 1f;
                src.volume = Mathf.Lerp(start, 0f, k);
                yield return null;
            }

            src.Stop();
            src.clip = null;
            src.volume = 0f;
            _bgmFadeRoutine = null;
        }

        #endregion

        #region Internals

        private static void ApplyVariantToSource(AudioSource src, SoundDatabase.ClipVariant v, float volumeScale, float? pitchOverride)
        {
            src.clip = v.clip;
            src.volume = Mathf.Clamp01(v.volume * volumeScale);
            src.loop = v.loop;

            float pitch = pitchOverride ?? 1f;
            if (v.usePitchRandom)
                pitch = UnityEngine.Random.Range(v.pitchRange.x, v.pitchRange.y);
            src.pitch = pitch;
        }

        private KeyState GetOrCreateState(Dictionary<string, KeyState> dict, string key)
        {
            if (!dict.TryGetValue(key, out var st))
            {
                st = new KeyState();
                dict[key] = st;
            }
            return st;
        }

        private bool IsOnCooldown(string key, float cooldown)
        {
            if (cooldown <= 0f) return false;
            float now = Time.time;
            if (_lastPlayAt.TryGetValue(key, out float last) && now - last < cooldown)
                return true;
            _lastPlayAt[key] = now;
            return false;
        }

        private IEnumerator ReleaseWhenFinished(string poolKey, AudioSource src)
        {
            var clip = src.clip;
            if (!clip)
            {
                PoolingManager.Instance.Release(poolKey, src);
                yield break;
            }

            // รอจนกว่าจะหยุดเล่น (กันเปลี่ยนคลิประหว่างทาง)
            while (src && src.isActiveAndEnabled && src.isPlaying)
                yield return null;

            if (src)
                PoolingManager.Instance.Release(poolKey, src);
        }

        private bool TryGetSfx(string key, out SoundDatabase.SFXEntry entry)
        {
            entry = default;
            if (string.IsNullOrEmpty(key) || database == null || database.SfxMap == null) return false;
            if (!database.SfxMap.TryGetValue(key, out entry))
            {
                Debug.LogWarning($"[SoundManager] Unknown SFX key: {key}");
                return false;
            }
            if (entry.variants == null || entry.variants.Count == 0)
            {
                Debug.LogWarning($"[SoundManager] SFX key '{key}' has no variants.");
                return false;
            }
            return true;
        }

        private bool TryGetUi(string key, out SoundDatabase.UIEntry entry)
        {
            entry = default;
            if (string.IsNullOrEmpty(key) || database == null || database.UiMap == null) return false;
            if (!database.UiMap.TryGetValue(key, out entry))
            {
                Debug.LogWarning($"[SoundManager] Unknown UI key: {key}");
                return false;
            }
            if (entry.variants == null || entry.variants.Count == 0)
            {
                Debug.LogWarning($"[SoundManager] UI key '{key}' has no variants.");
                return false;
            }
            return true;
        }

        private bool TryGetBgm(string key, out SoundDatabase.BGMEntry entry)
        {
            entry = default;
            if (string.IsNullOrEmpty(key) || database == null || database.BgmMap == null) return false;
            if (!database.BgmMap.TryGetValue(key, out entry))
            {
                Debug.LogWarning($"[SoundManager] Unknown BGM key: {key}");
                return false;
            }
            if (entry.clip == null)
            {
                Debug.LogWarning($"[SoundManager] BGM key '{key}' has no clip.");
                return false;
            }
            return true;
        }

        #endregion

        #region Convenience wrappers (optional)

        // SFX ชนิด 2D (บังคับ) — เผื่ออยากข้าม spatial ของ entry
        public AudioSource PlaySFX2D(string key, float volumeScale = 1f, float? pitchOverride = null)
        {
            if (!TryGetSfx(key, out var entry)) return null;
            // hack: เล่นผ่าน UI pool เพื่อ 2D แน่ ๆ
            var state = GetOrCreateState(_sfxStates, key);
            int picked = entry.PickIndex(ref state.lastIndex, ref state.shuffleBag);
            if (picked < 0 || picked >= entry.variants.Count) return null;

            var variant = entry.variants[picked];
            if (!variant.clip) return null;

            var src = PoolingManager.Instance.Get<AudioSource>(POOL_UI);
            if (!src) return null;

            ApplyVariantToSource(src, variant, volumeScale, pitchOverride);
            src.spatialBlend = 0f;
            src.transform.localPosition = Vector3.zero;
            src.outputAudioMixerGroup = entry.mixerOverride ? entry.mixerOverride : (defaultUiMixer ? defaultUiMixer : defaultSfxMixer);
            src.Play();

            if (!src.loop)
                StartCoroutine(ReleaseWhenFinished(POOL_UI, src));

            return src;
        }

        #endregion
    }
}
