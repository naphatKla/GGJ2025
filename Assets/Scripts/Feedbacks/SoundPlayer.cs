// SoundPlayer.cs

using System.Collections.Generic;
using Manager.SoundManager;
using Sirenix.OdinInspector;
using UnityEngine;

// SoundManager, SoundDatabase, SoundName

namespace Feedbacks
{
    [AddComponentMenu("Audio/Sound Player")]
    [DisallowMultipleComponent]
    public class SoundPlayer : MonoBehaviour
    {
        public enum Category
        {
            SFX = 0,
            UI  = 1,
            BGM = 2,
        }
        
        [LabelText("Category")]
        [SerializeField] private Category category = Category.SFX;

        // ====== Key (เลือกด้วย Short name แล้ว resolve เป็น Full key ตอนเล่น) ======
        [LabelText("Key (Short)")]
        [ValidateInput(nameof(ValidateShortKey), "Unknown key for selected category.", InfoMessageType.Error)]
        [ValueDropdown(nameof(__ShortKeyDropdown))]
        [SerializeField] private string keyShort;
        
        private string FullKey => ResolveFullKey();

        // ====== Common options (ไม่มีพารามิเตอร์ตอนเรียกเล่น แต่ปรับผ่าน Inspector) ======
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

        // === last played source (สำหรับ SFX/UI ที่อาจ loop และต้อง Stop/Release เอง) ===
        private AudioSource _lastSource;

        // ---------- Lifecycle ----------
        private void OnEnable()
        {
            if (playOnEnable)
                Play();
        }

        private void OnDisable()
        {
            if (stopOnDisable)
                Stop();
        }

        // ---------- Public API (no-params; เรียกจาก UnityEvent ได้) ----------
        [Button("Play")]
        public void Play()
        {
            var sm = SoundManager.Instance;
            if (sm == null)
            {
                Debug.LogWarning("[SoundPlayer] SoundManager instance not found.");
                return;
            }

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

                    _lastSource = sm.PlaySFX(
                        fullKey,
                        worldPos,
                        volumeScale,
                        usePitchOverride ? (float?)pitch : null
                    );
                    break;
                }

                case Category.UI:
                {
                    _lastSource = sm.PlayUI(
                        fullKey,
                        volumeScale,
                        usePitchOverride ? (float?)pitch : null
                    );
                    break;
                }

                case Category.BGM:
                {
                    sm.PlayBGM(
                        fullKey,
                        overrideBgmFade ? (float?)bgmFadeOut : null,
                        overrideBgmFade ? (float?)bgmFadeIn  : null
                    );
                    // BGM channel managed internally; no _lastSource needed
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
                    if (_lastSource) sm.StopSFX(_lastSource, release: true);
                    _lastSource = null;
                    break;

                case Category.UI:
                    if (_lastSource) sm.StopUI(_lastSource, release: true);
                    _lastSource = null;
                    break;

                case Category.BGM:
                    sm.StopBGM(overrideBgmFade ? (float?)bgmFadeOut : null);
                    break;
            }
        }

        // ---------- Helpers ----------
        private string ResolveFullKey()
        {
            string group = category.ToString(); // "SFX" / "UI" / "BGM"
            if (string.IsNullOrEmpty(keyShort))
                return "";
            // แปลง Short label -> Full key ตามกลุ่ม
            var full = SoundName.ResolveFullKey(group, keyShort);
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
            // clamp values
            volumeScale = Mathf.Clamp01(volumeScale);
            if (usePitchOverride) pitch = Mathf.Clamp(pitch, 0.1f, 3f);

            // keep FullKey preview up-to-date in the inspector
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this) // object still alive?
                    UnityEditor.EditorUtility.SetDirty(this);
            };
        }
#endif
    }
}
