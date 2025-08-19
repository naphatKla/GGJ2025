// SoundManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using MoreMountains.Tools;
using Manager;                  // PoolingManager + PoolingGroupName
using Manager.SoundManager;     // SoundDatabase, SoundName

namespace Manager.SoundManager
{
    /// <summary>
    /// เล่น SFX / UI / BGM จาก SoundDatabase (รุ่นที่ไม่มี Map/Rebuild ในตัว)
    /// - สร้างแคชภายในจากลิสต์ SFX/UI/BGM เมื่อ Awake()
    /// - รองรับหลายคลิป/โหมดสุ่ม/คูลดาวน์/3D SFX และ 2D UI
    /// - BGM crossfade 2 แชนแนล
    /// - ใช้ AudioSource pooling ผ่าน PoolingManager
    /// </summary>
    public class SoundManager : MMSingleton<SoundManager>
    {
        [Header("Database")]
        [SerializeField] private SoundDatabase database;

        [Header("Mixer Defaults")]
        [SerializeField] private AudioMixerGroup defaultSfxMixer;
        [SerializeField] private AudioMixerGroup defaultUiMixer;
        [SerializeField] private AudioMixerGroup defaultBgmMixer;

        [Header("Pools")]
        [SerializeField, Min(1)] private int sfxDefaultCapacity = 16;
        [SerializeField, Min(1)] private int sfxMaxSize = 100;
        [SerializeField, Min(1)] private int uiDefaultCapacity = 16;
        [SerializeField, Min(1)] private int uiMaxSize = 64;

        [Header("BGM Crossfade")]
        [SerializeField, Range(0f, 5f)] private float defaultFadeIn = 0.6f;
        [SerializeField, Range(0f, 5f)] private float defaultFadeOut = 0.6f;

        private const string POOL_SFX = "SFX_AudioSource";
        private const string POOL_UI  = "UI_AudioSource";

        // ======== runtime caches from database (เพราะ DB ไม่มี Map แล้ว) ========
        private readonly Dictionary<string, SoundDatabase.SFXEntry> _sfxMap =
            new Dictionary<string, SoundDatabase.SFXEntry>(System.StringComparer.Ordinal);
        private readonly Dictionary<string, SoundDatabase.UIEntry> _uiMap =
            new Dictionary<string, SoundDatabase.UIEntry>(System.StringComparer.Ordinal);
        private readonly Dictionary<string, SoundDatabase.BGMEntry> _bgmMap =
            new Dictionary<string, SoundDatabase.BGMEntry>(System.StringComparer.Ordinal);

        // cooldown
        private readonly Dictionary<string, float> _lastPlayAt =
            new Dictionary<string, float>(System.StringComparer.Ordinal);

        // random state per key
        private class KeyState
        {
            public int lastIndex = -1;
            public List<int> shuffleBag;
        }
        private readonly Dictionary<string, KeyState> _sfxStates = new(System.StringComparer.Ordinal);
        private readonly Dictionary<string, KeyState> _uiStates  = new(System.StringComparer.Ordinal);

        // BGM
        private AudioSource _bgmA, _bgmB, _bgmActive;
        private Coroutine _bgmFadeRoutine;

        // RNG
        private static readonly System.Random _rnd = new System.Random();
        public SoundDatabase Database => database;

        protected override void Awake()
        {
            base.Awake();

            if (database == null)
            {
                Debug.LogError("[SoundManager] SoundDatabase is not assigned.");
            }
            else
            {
                RebuildLocalCachesFromDatabase();
            }

            EnsureSfxPool();
            EnsureUiPool();
            EnsureBgmChannels();
        }

        #region Build Local Caches
        private void RebuildLocalCachesFromDatabase()
        {
            _sfxMap.Clear();
            _uiMap.Clear();
            _bgmMap.Clear();

            // สร้างแคชจากลิสต์ใน DB; ข้าม entry ที่ key ว่าง/ไม่ตรง SoundName หรือไม่มีคลิป
            if (database.sfx != null)
            {
                foreach (var e in database.sfx)
                {
                    if (string.IsNullOrEmpty(e.Key)) continue;
                    if (!SoundName.IsValid(e.Key)) continue;
                    if (e.variants == null || e.variants.Count == 0) continue;
                    bool anyClip = false;
                    foreach (var v in e.variants) { if (v.clip) { anyClip = true; break; } }
                    if (!anyClip) continue;

                    if (!_sfxMap.ContainsKey(e.Key))
                        _sfxMap.Add(e.Key, e);
                }
            }

            if (database.ui != null)
            {
                foreach (var e in database.ui)
                {
                    if (string.IsNullOrEmpty(e.Key)) continue;
                    if (!SoundName.IsValid(e.Key)) continue;
                    if (e.variants == null || e.variants.Count == 0) continue;
                    bool anyClip = false;
                    foreach (var v in e.variants) { if (v.clip) { anyClip = true; break; } }
                    if (!anyClip) continue;

                    if (!_uiMap.ContainsKey(e.Key))
                        _uiMap.Add(e.Key, e);
                }
            }

            if (database.bgm != null)
            {
                foreach (var e in database.bgm)
                {
                    if (string.IsNullOrEmpty(e.Key)) continue;
                    if (!SoundName.IsValid(e.Key)) continue;
                    if (e.clip == null) continue;

                    if (!_bgmMap.ContainsKey(e.Key))
                        _bgmMap.Add(e.Key, e);
                }
            }
        }
        #endregion

        #region Pools
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
                onGet: s => s.gameObject.SetActive(true),
                onRelease: s =>
                {
                    if (!s) return;
                    s.Stop();
                    s.clip = null;
                    s.loop = false;
                    s.pitch = 1f;
                    s.spatialBlend = 0f;
                    s.minDistance = 1f;
                    s.maxDistance = 500f;
                    s.rolloffMode = AudioRolloffMode.Logarithmic;
                    s.transform.localPosition = Vector3.zero;
                    s.outputAudioMixerGroup = defaultSfxMixer;
                    s.gameObject.SetActive(false);
                },
                onDestroy: s => { if (s) Destroy(s.gameObject); },
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
                    src.spatialBlend = 0f;
                    src.outputAudioMixerGroup = defaultUiMixer ? defaultUiMixer : defaultSfxMixer;
                    return src;
                },
                onGet: s => s.gameObject.SetActive(true),
                onRelease: s =>
                {
                    if (!s) return;
                    s.Stop();
                    s.clip = null;
                    s.loop = false;
                    s.pitch = 1f;
                    s.spatialBlend = 0f;
                    s.transform.localPosition = Vector3.zero;
                    s.outputAudioMixerGroup = defaultUiMixer ? defaultUiMixer : defaultSfxMixer;
                    s.gameObject.SetActive(false);
                },
                onDestroy: s => { if (s) Destroy(s.gameObject); },
                defaultCapacity: uiDefaultCapacity,
                maxSize: uiMaxSize
            );
        }
        #endregion

        #region SFX API
        public AudioSource PlaySFX(string key, Vector3? worldPos = null, float volumeScale = 1f, float? pitchOverride = null)
        {
            if (!TryGetSfx(key, out var entry)) return null;

            if (IsOnCooldown(key, entry.cooldown)) return null;

            var state = GetOrCreateState(_sfxStates, key);
            int picked = PickIndex(entry.variants, entry.pickMode, entry.avoidImmediateRepeat, ref state.lastIndex, ref state.shuffleBag);
            if (picked < 0 || picked >= entry.variants.Count) return null;

            var variant = entry.variants[picked];
            if (!variant.clip) return null;

            var src = PoolingManager.Instance.Get<AudioSource>(POOL_SFX);
            if (!src) return null;

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

        #region UI API
        public AudioSource PlayUI(string key, float volumeScale = 1f, float? pitchOverride = null)
        {
            if (!TryGetUi(key, out var entry)) return null;

            if (IsOnCooldown(key, entry.cooldown)) return null;

            var state = GetOrCreateState(_uiStates, key);
            int picked = PickIndex(entry.variants, entry.pickMode, entry.avoidImmediateRepeat, ref state.lastIndex, ref state.shuffleBag);
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
            _bgmFadeRoutine = StartCoroutine(CrossFadeBGM(_bgmActive, next, fo, fi, Mathf.Clamp01(cfg.volume)));

            _bgmActive = next;
        }

        public void StopBGM(float? fadeOut = null)
        {
            float fo = fadeOut ?? defaultFadeOut;
            if (_bgmFadeRoutine != null) StopCoroutine(_bgmFadeRoutine);
            if (_bgmActive) StartCoroutine(FadeOutThenStop(_bgmActive, fo));
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

        #region Internals (pick/cooldown/helpers)
        private static void ApplyVariantToSource(AudioSource src, SoundDatabase.ClipVariant v, float volumeScale, float? pitchOverride)
        {
            src.clip = v.clip;
            src.volume = Mathf.Clamp01(v.volume * volumeScale);
            src.loop = v.loop;

            // ถ้าเปิด random pitch ใน variant -> ใช้ช่วงที่กำหนด; ถ้าไม่เปิดและมี pitchOverride -> ใช้ override
            float pitch = 1f;
            if (v.usePitchRandom)
                pitch = Random.Range(v.pitchRange.x, v.pitchRange.y);
            else if (pitchOverride.HasValue)
                pitch = pitchOverride.Value;

            src.pitch = pitch;
        }

        private static int PickIndex(
            List<SoundDatabase.ClipVariant> variants,
            SoundDatabase.RandomPickMode mode,
            bool avoidImmediateRepeat,
            ref int lastIndex,
            ref List<int> shuffleBag)
        {
            if (variants == null || variants.Count == 0) return -1;

            switch (mode)
            {
                case SoundDatabase.RandomPickMode.First:
                    lastIndex = 0; return 0;

                case SoundDatabase.RandomPickMode.Sequential:
                    lastIndex = (lastIndex + 1) % variants.Count; return lastIndex;

                case SoundDatabase.RandomPickMode.ShuffleNoRepeat:
                    if (shuffleBag == null || shuffleBag.Count == 0)
                    {
                        shuffleBag ??= new List<int>(variants.Count);
                        shuffleBag.Clear();
                        for (int i = 0; i < variants.Count; i++) shuffleBag.Add(i);
                        // Fisher–Yates
                        for (int i = 0; i < shuffleBag.Count; i++)
                        {
                            int j = _rnd.Next(i, shuffleBag.Count);
                            (shuffleBag[i], shuffleBag[j]) = (shuffleBag[j], shuffleBag[i]);
                        }
                    }
                    int idx = shuffleBag[^1];
                    shuffleBag.RemoveAt(shuffleBag.Count - 1);
                    lastIndex = idx; return idx;

                case SoundDatabase.RandomPickMode.WeightedRandom:
                {
                    float total = 0f;
                    for (int i = 0; i < variants.Count; i++)
                        total += Mathf.Max(0f, variants[i].weight);

                    if (total <= 0f)
                        goto case SoundDatabase.RandomPickMode.Random;

                    float pick = (float)_rnd.NextDouble() * total;
                    float acc = 0f;
                    for (int i = 0; i < variants.Count; i++)
                    {
                        acc += Mathf.Max(0f, variants[i].weight);
                        if (pick <= acc)
                        {
                            int chosen = i;
                            if (avoidImmediateRepeat && chosen == lastIndex && variants.Count > 1)
                                chosen = (chosen + 1) % variants.Count;
                            lastIndex = chosen;
                            return chosen;
                        }
                    }
                    lastIndex = variants.Count - 1; return lastIndex;
                }

                case SoundDatabase.RandomPickMode.RandomNoImmediateRepeat:
                case SoundDatabase.RandomPickMode.Random:
                default:
                {
                    int c = variants.Count;
                    if (c == 1) { lastIndex = 0; return 0; }
                    int i = _rnd.Next(0, c);
                    if (avoidImmediateRepeat && i == lastIndex)
                        i = (i + 1) % c;
                    lastIndex = i;
                    return i;
                }
            }
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
            while (src && src.isActiveAndEnabled && src.isPlaying)
                yield return null;

            if (src)
                PoolingManager.Instance.Release(poolKey, src);
        }

        private bool TryGetSfx(string key, out SoundDatabase.SFXEntry entry)
        {
            entry = default;
            if (string.IsNullOrEmpty(key) || database == null) return false;

            if (_sfxMap.TryGetValue(key, out entry)) return true;

            // เผื่อ DB ถูกแก้ไขระหว่างรัน (rare) — rebuild แล้วลองใหม่
            RebuildLocalCachesFromDatabase();
            return _sfxMap.TryGetValue(key, out entry);
        }

        private bool TryGetUi(string key, out SoundDatabase.UIEntry entry)
        {
            entry = default;
            if (string.IsNullOrEmpty(key) || database == null) return false;

            if (_uiMap.TryGetValue(key, out entry)) return true;

            RebuildLocalCachesFromDatabase();
            return _uiMap.TryGetValue(key, out entry);
        }

        private bool TryGetBgm(string key, out SoundDatabase.BGMEntry entry)
        {
            entry = default;
            if (string.IsNullOrEmpty(key) || database == null) return false;

            if (_bgmMap.TryGetValue(key, out entry)) return true;

            RebuildLocalCachesFromDatabase();
            return _bgmMap.TryGetValue(key, out entry);
        }
        #endregion
    }
}
