// SoundPlayer.cs
using System.Collections.Generic;
using Manager.SoundManager;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Feedbacks
{
    [AddComponentMenu("Audio/Sound Player")]
    public class SoundPlayer : MonoBehaviour
    {
        public enum Category { SFX = 0, UI = 1, BGM = 2 }

        [LabelText("Category")]
        [SerializeField] protected Category category = Category.SFX;

        // ====== Key หลัก (Short) ======
        [LabelText("Key (Short)")]
        [ValidateInput(nameof(ValidateShortKey), "Unknown key for selected category.", InfoMessageType.Error)]
        [ValueDropdown(nameof(__ShortKeyDropdown))]
        [SerializeField] private string keyShort;

        private string FullKey => ResolveFullKey();

        // ====== Extra Keys (เล่นพร้อมกัน) ======
        [System.Serializable]
        public struct ExtraKey
        {
            [LabelText("Category")]
            [SerializeField] public Category category;

            [LabelText("Key (Short)")]
            [ValueDropdown(nameof(__ShortKeyDropdown))]
            public string keyShort;

            // dropdown อิงจาก category ภายใน element เอง
            private IEnumerable<ValueDropdownItem<string>> __ShortKeyDropdown()
            {
#if UNITY_EDITOR
                string group = category.ToString();
                return SoundName.Odin.ShortGroup(group);
#else
                return System.Array.Empty<ValueDropdownItem<string>>();
#endif
            }
        }

        [ShowIf(nameof(IsSfxOrUi))]
        [TitleGroup("Extra Keys")]
        [LabelText("Play Extra Keys Together")]
        public bool useExtraKeys = false;

        [ShowIf("@useExtraKeys && IsSfxOrUi()")]
        [ListDrawerSettings(Expanded = true, DraggableItems = true)]
        public List<ExtraKey> extraKeys = new();

        // ====== Common options ======
        [Title("Settings")]
        [LabelText("Volume Scale"), Range(0f, 2f)]
        public float volumeScale = 1f;

        [LabelText("Play On Enable")] public bool playOnEnable;
        [LabelText("Stop On Disable")] public bool stopOnDisable;

        [LabelText("Override Pitch")] public bool usePitchOverride = false;
        [ShowIf(nameof(usePitchOverride))]
        [LabelText("Pitch"), Range(0.1f, 3f)]
        public float pitch = 1f;

        [LabelText("Time Scale Mode")]
        public SoundManager.TimeScaleMode timeScaleMode = SoundManager.TimeScaleMode.Unscaled;

        // --- SFX Fade ---
        [ShowIf(nameof(IsSfx))]
        [LabelText("Use Fade In")]
        public bool sfxUseFadeIn = false;

        [ShowIf("@IsSfx() && sfxUseFadeIn")]
        [LabelText("Fade In (sec)"), MinValue(0f)]
        public float sfxFadeIn = 0.08f;

        [ShowIf(nameof(IsSfx))]
        [LabelText("Fade Uses Scaled Time (In)")]
        public bool sfxFadeInUsesScaledTime = false;

        [ShowIf(nameof(IsSfx))]
        [LabelText("Fade Out On Stop")]
        public bool sfxFadeOutOnStop = true;

        [ShowIf("@IsSfx() && sfxFadeOutOnStop")]
        [LabelText("Fade Out (sec)"), MinValue(0f)]
        public float sfxFadeOut = 0.10f;

        [ShowIf("@IsSfx() && sfxFadeOutOnStop")]
        [LabelText("Fade Uses Scaled Time (Out)")]
        public bool sfxFadeOutUsesScaledTime = false;

        // ====== UI-only (2D) ======
        [ShowIf(nameof(IsUi))]
        [LabelText("Use Fade In")]
        public bool uiUseFadeIn = false;

        [ShowIf("@IsUi() && uiUseFadeIn")]
        [LabelText("Fade In (sec)"), MinValue(0f)]
        public float uiFadeIn = 0.06f;

        [ShowIf(nameof(IsUi))]
        [LabelText("Fade Uses Scaled Time (In)")]
        public bool uiFadeInUsesScaledTime = false;

        [ShowIf(nameof(IsUi))]
        [LabelText("Fade Out On Stop")]
        public bool uiFadeOutOnStop = false;

        [ShowIf("@IsUi() && uiFadeOutOnStop")]
        [LabelText("Fade Out (sec)"), MinValue(0f)]
        public float uiFadeOut = 0.08f;

        [ShowIf("@IsUi() && uiFadeOutOnStop")]
        [LabelText("Fade Uses Scaled Time (Out)")]
        public bool uiFadeOutUsesScaledTime = false;

        // ====== BGM-only ======
        [ShowIf(nameof(IsBgm))]
        [LabelText("Override Fade In/Out")]
        public bool overrideBgmFade = false;

        [ShowIf("@IsBgm() && overrideBgmFade")]
        [LabelText("Fade Out (sec)"), MinValue(0f)]
        public float bgmFadeOut = 0.6f;

        [ShowIf("@IsBgm() && overrideBgmFade")]
        [LabelText("Fade In (sec)"), MinValue(0f)]
        public float bgmFadeIn = 0.6f;

        [ShowIf(nameof(IsBgm))]
        [LabelText("Fade Uses Scaled Time")]
        public bool bgmFadeUsesScaledTime = false;

        // ====== SFX-only ======
        [ShowIf(nameof(IsSfx))] [PropertySpace]
        [LabelText("Use Transform Position")]
        public bool useTransformPosition = true;

        [ShowIf("@IsSfx() && useTransformPosition")]
        [LabelText("Position Source (optional)")] [PropertySpace(spaceAfter:20f,spaceBefore:0)]
        public Transform positionSource;

        [ShowIf("@IsSfx() && !useTransformPosition")]
        [LabelText("Custom World Position")] [PropertySpace(spaceAfter:20f,spaceBefore:0)]
        public Vector3 customWorldPosition;

        // เก็บ source แยกตามชนิด
        private readonly List<AudioSource> _activeSfxSources = new(8);
        private readonly List<AudioSource> _activeUiSources  = new(8);

        // ---------- Lifecycle ----------
        private void OnEnable()
        {
            if (playOnEnable) Play();
        }

        private void OnDisable()
        {
            if (stopOnDisable) Stop();
        }

        // ---------- Public API ----------
        [Button("Play")]
        public void Play()
        {
            var sm = SoundManager.Instance;
            if (sm == null)
            {
                Debug.LogWarning("[SoundPlayer] SoundManager instance not found.");
                return;
            }

            CleanupDeadRefs();

            var fullKey = FullKey;
            if (string.IsNullOrEmpty(fullKey))
            {
                Debug.LogWarning("[SoundPlayer] Key is empty or invalid.");
                return;
            }

            switch (category)
            {
                case Category.SFX:
                {
                    Vector3? worldPos = useTransformPosition
                        ? (positionSource ? positionSource.position : transform.position)
                        : (Vector3?)customWorldPosition;

                    AudioSource src = sfxUseFadeIn
                        ? sm.PlaySFXFadeIn(fullKey, sfxFadeIn, worldPos, volumeScale, usePitchOverride ? (float?)pitch : null, timeScaleMode, sfxFadeInUsesScaledTime)
                        : sm.PlaySFX(fullKey, worldPos, volumeScale, usePitchOverride ? (float?)pitch : null, timeScaleMode);

                    if (src) _activeSfxSources.Add(src);
                    break;
                }

                case Category.UI:
                {
                    AudioSource src = uiUseFadeIn
                        ? sm.PlayUIFadeIn(fullKey, uiFadeIn, volumeScale, usePitchOverride ? (float?)pitch : null, timeScaleMode, uiFadeInUsesScaledTime)
                        : sm.PlayUI(fullKey, volumeScale, usePitchOverride ? (float?)pitch : null, timeScaleMode);

                    if (src) _activeUiSources.Add(src);
                    break;
                }

                case Category.BGM:
                {
                    sm.PlayBGM(fullKey,
                        overrideBgmFade ? (float?)bgmFadeOut : null,
                        overrideBgmFade ? (float?)bgmFadeIn  : null,
                        timeScaleMode,
                        bgmFadeUsesScaledTime);
                    break;
                }
            }

            // ---- Extra keys (เล่นพร้อมกัน) ----
            if (useExtraKeys && extraKeys != null)
            {
                foreach (var ek in extraKeys)
                {
                    if (string.IsNullOrEmpty(ek.keyShort)) continue;
                    var extraFull = ResolveFullKey(ek.category, ek.keyShort);
                    if (string.IsNullOrEmpty(extraFull)) continue;

                    switch (ek.category)
                    {
                        case Category.SFX:
                        {
                            Vector3? worldPos = useTransformPosition
                                ? (positionSource ? positionSource.position : transform.position)
                                : (Vector3?)customWorldPosition;

                            var s = sfxUseFadeIn
                                ? sm.PlaySFXFadeIn(extraFull, sfxFadeIn, worldPos, volumeScale, usePitchOverride ? (float?)pitch : null, timeScaleMode, sfxFadeInUsesScaledTime)
                                : sm.PlaySFX(extraFull, worldPos, volumeScale, usePitchOverride ? (float?)pitch : null, timeScaleMode);
                            if (s) _activeSfxSources.Add(s);
                            break;
                        }

                        case Category.UI:
                        {
                            var u = uiUseFadeIn
                                ? sm.PlayUIFadeIn(extraFull, uiFadeIn, volumeScale, usePitchOverride ? (float?)pitch : null, timeScaleMode, uiFadeInUsesScaledTime)
                                : sm.PlayUI(extraFull, volumeScale, usePitchOverride ? (float?)pitch : null, timeScaleMode);
                            if (u) _activeUiSources.Add(u);
                            break;
                        }

                        case Category.BGM:
                        {
                            sm.PlayBGM(extraFull,
                                overrideBgmFade ? (float?)bgmFadeOut : null,
                                overrideBgmFade ? (float?)bgmFadeIn  : null,
                                timeScaleMode,
                                bgmFadeUsesScaledTime);
                            break;
                        }
                    }
                }
            }
        }

        [Button("Stop")]
        public void Stop()
        {
            var sm = SoundManager.Instance;
            if (sm == null) return;

            CleanupDeadRefs();

            switch (category)
            {
                case Category.SFX:
                    for (int i = 0; i < _activeSfxSources.Count; i++)
                    {
                        var s = _activeSfxSources[i];
                        if (!s) continue;

                        if (sfxFadeOutOnStop) sm.FadeOutSFX(s, sfxFadeOut, release: true, useScaledTime: sfxFadeOutUsesScaledTime);
                        else sm.StopSFX(s, release: true);
                    }
                    _activeSfxSources.Clear();
                    break;

                case Category.UI:
                    for (int i = 0; i < _activeUiSources.Count; i++)
                    {
                        var u = _activeUiSources[i];
                        if (!u) continue;

                        if (uiFadeOutOnStop) sm.FadeOutUI(u, uiFadeOut, release: true, useScaledTime: uiFadeOutUsesScaledTime);
                        else sm.StopUI(u, release: true);
                    }
                    _activeUiSources.Clear();
                    break;

                case Category.BGM:
                    sm.StopBGM(overrideBgmFade ? (float?)bgmFadeOut : null, fadeUsesScaledTime: bgmFadeUsesScaledTime);
                    break;
            }
        }

        private void CleanupDeadRefs()
        {
            for (int i = _activeSfxSources.Count - 1; i >= 0; i--)
                if (_activeSfxSources[i] == null) _activeSfxSources.RemoveAt(i);

            for (int i = _activeUiSources.Count - 1; i >= 0; i--)
                if (_activeUiSources[i] == null) _activeUiSources.RemoveAt(i);
        }

        // ---------- Helpers ----------
        private string ResolveFullKey() => ResolveFullKey(category, keyShort);

        private static string ResolveFullKey(Category cat, string shortKey)
        {
            if (string.IsNullOrEmpty(shortKey)) return "";
            string group = cat.ToString();
            var full = SoundName.ResolveFullKey(group, shortKey);
            return SoundName.IsValid(full) ? full : "";
        }

        private bool ValidateShortKey(string shortName)
        {
            if (string.IsNullOrEmpty(shortName)) return false;
            string group = category.ToString();
            string full = SoundName.ResolveFullKey(group, shortName);
            return SoundName.IsValid(full);
        }

        private bool IsSfx() => category == Category.SFX;
        private bool IsUi()  => category == Category.UI;
        private bool IsBgm() => category == Category.BGM;
        private bool IsSfxOrUi() => IsSfx() || IsUi();

        // Dropdown ของ key หลัก
        private IEnumerable<ValueDropdownItem<string>> __ShortKeyDropdown()
        {
#if UNITY_EDITOR
            string group = category.ToString();
            return SoundName.Odin.ShortGroup(group);
#else
            return System.Array.Empty<ValueDropdownItem<string>>();
#endif
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            volumeScale = Mathf.Clamp(volumeScale, 0f, 2f);
            if (usePitchOverride) pitch = Mathf.Clamp(pitch, 0.1f, 3f);

            sfxFadeIn   = Mathf.Max(0f, sfxFadeIn);
            sfxFadeOut  = Mathf.Max(0f, sfxFadeOut);
            uiFadeIn    = Mathf.Max(0f, uiFadeIn);
            uiFadeOut   = Mathf.Max(0f, uiFadeOut);
        }
#endif
    }
}
