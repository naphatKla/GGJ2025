// SoundPlayer.cs
using System.Collections.Generic;
using Manager.SoundManager;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Feedbacks
{
    [AddComponentMenu("Audio/Sound Player")]
    [DisallowMultipleComponent]
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
        
        [System.Serializable]
        public struct ExtraKey
        {
            [SerializeField] private Category category;
            
            [LabelText("Key (Short)")]
            [ValueDropdown(nameof(__ShortKeyDropdown))]
            public string keyShort;
            
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
        [TitleGroup("Extra Keys (SFX/UI)")]
        [LabelText("Play Extra Keys Together")]
        public bool useExtraKeys = false;

        [ShowIf("@useExtraKeys && IsSfxOrUi()")]
        [ListDrawerSettings(Expanded = true, DraggableItems = true)]
        public List<ExtraKey> extraKeys = new();

        // ====== Common options ======
        [Title("Settings")]
        [LabelText("Play On Enable")] public bool playOnEnable;
        [LabelText("Stop On Disable")] public bool stopOnDisable;

        [LabelText("Volume Scale"), Range(0f, 2f)]
        public float volumeScale = 1f;

        [LabelText("Override Pitch")] public bool usePitchOverride = false;
        [ShowIf(nameof(usePitchOverride))]
        [LabelText("Pitch"), Range(0.1f, 3f)]
        public float pitch = 1f;

        // ====== SFX-only ======
        [ShowIf(nameof(IsSfx))]
        [LabelText("Use Transform Position")]
        public bool useTransformPosition = true;

        [ShowIf("@IsSfx() && useTransformPosition")]
        [LabelText("Position Source (optional)")]
        public Transform positionSource;

        [ShowIf("@IsSfx() && !useTransformPosition")]
        [LabelText("Custom World Position")]
        public Vector3 customWorldPosition;

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

        // แทน _lastSource → รองรับหลายแหล่งเสียง
        private readonly List<AudioSource> _activeSources = new(8);

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

            // เคลียร์รายการก่อนเล่นใหม่ (กันค้าง)
            CleanupActiveSources();

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
                    Vector3? worldPos = null;
                    if (useTransformPosition)
                    {
                        var t = positionSource ? positionSource : transform;
                        worldPos = t.position;
                    }
                    else
                    {
                        worldPos = customWorldPosition;
                    }

                    // หลัก
                    var src = sm.PlaySFX(
                        fullKey,
                        worldPos,
                        volumeScale,
                        usePitchOverride ? (float?)pitch : null
                    );
                    if (src) _activeSources.Add(src);

                    // เพิ่มเติม (พร้อมกัน)
                    if (useExtraKeys && extraKeys != null)
                    {
                        foreach (var ek in extraKeys)
                        {
                            if (string.IsNullOrEmpty(ek.keyShort)) continue;
                            var extraFull = ResolveFullKey(category, ek.keyShort);
                            if (string.IsNullOrEmpty(extraFull)) continue;

                            var s = sm.PlaySFX(
                                extraFull,
                                worldPos,
                                volumeScale,
                                usePitchOverride ? (float?)pitch : null
                            );
                            if (s) _activeSources.Add(s);
                        }
                    }
                    break;
                }

                case Category.UI:
                {
                    // หลัก
                    var src = sm.PlayUI(
                        fullKey,
                        volumeScale,
                        usePitchOverride ? (float?)pitch : null
                    );
                    if (src) _activeSources.Add(src);

                    // เพิ่มเติม (พร้อมกัน)
                    if (useExtraKeys && extraKeys != null)
                    {
                        foreach (var ek in extraKeys)
                        {
                            if (string.IsNullOrEmpty(ek.keyShort)) continue;
                            var extraFull = ResolveFullKey(category, ek.keyShort);
                            if (string.IsNullOrEmpty(extraFull)) continue;

                            var s = sm.PlayUI(
                                extraFull,
                                volumeScale,
                                usePitchOverride ? (float?)pitch : null
                            );
                            if (s) _activeSources.Add(s);
                        }
                    }
                    break;
                }

                case Category.BGM:
                {
                    SoundManager.Instance.PlayBGM(
                        fullKey,
                        overrideBgmFade ? (float?)bgmFadeOut : null,
                        overrideBgmFade ? (float?)bgmFadeIn  : null
                    );
                    break;
                }
            }
        }

        [Button("Stop")]
        public void Stop()
        {
            var sm = SoundManager.Instance;
            if (sm == null) return;

            switch (category)
            {
                case Category.SFX:
                    for (int i = 0; i < _activeSources.Count; i++)
                        if (_activeSources[i]) sm.StopSFX(_activeSources[i], release: true);
                    _activeSources.Clear();
                    break;

                case Category.UI:
                    for (int i = 0; i < _activeSources.Count; i++)
                        if (_activeSources[i]) sm.StopUI(_activeSources[i], release: true);
                    _activeSources.Clear();
                    break;

                case Category.BGM:
                    sm.StopBGM(overrideBgmFade ? (float?)bgmFadeOut : null);
                    break;
            }
        }

        private void CleanupActiveSources()
        {
            // ลบ element ที่โดน Destroy ไปแล้วออกจากลิสต์
            for (int i = _activeSources.Count - 1; i >= 0; i--)
            {
                if (_activeSources[i] == null)
                    _activeSources.RemoveAt(i);
            }
        }

        // ---------- Helpers ----------
        private string ResolveFullKey()
            => ResolveFullKey(category, keyShort);

        private static string ResolveFullKey(Category cat, string shortKey)
        {
            if (string.IsNullOrEmpty(shortKey)) return "";
            string group = cat.ToString(); // "SFX" / "UI" / "BGM"
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

        // ShowIf shorthand
        private bool IsSfx() => category == Category.SFX;
        private bool IsBgm() => category == Category.BGM;
        private bool IsSfxOrUi() => category == Category.SFX || category == Category.UI;

        // Odin: dynamic dropdown per category (Editor only)
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
        }
#endif
    }
}
