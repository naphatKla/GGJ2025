using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;

namespace Manager.SoundManager
{
    [CreateAssetMenu(fileName = "SoundDatabase", menuName = "Audio/Sound Database", order = 0)]
    public class SoundDatabase : SerializedScriptableObject
    {
        // =========================
        // Common
        // =========================
        public enum RandomPickMode
        {
            First = 0,                // เล่นตัวแรกเสมอ (debug/use case เฉพาะ)
            Random = 1,               // สุ่มเท่ากันทุกตัว
            WeightedRandom = 2,       // สุ่มตาม weight
            Sequential = 3,           // เล่นวนตามลำดับ 0..n
            ShuffleNoRepeat = 4,      // สับไพ่ 1 รอบ ครบค่อยสับใหม่
            RandomNoImmediateRepeat = 5, // สุ่ม แต่กันติดกันซ้ำตัวเดิม
        }

        [Serializable]
        public struct ClipVariant
        {
            [HorizontalGroup("row", Width = 220)]
            [HideLabel] public AudioClip clip;

            [HorizontalGroup("row"), LabelText("Vol"), Range(0f, 1f)]
            public float volume;

            [HorizontalGroup("row"), LabelText("Loop")]
            public bool loop;

            [HorizontalGroup("row2"), LabelText("Rnd Pitch")]
            public bool usePitchRandom;

            [HorizontalGroup("row2"), LabelText("Pitch"), ShowIf(nameof(usePitchRandom))]
            public Vector2 pitchRange; // e.g. (0.95, 1.05)

            [HorizontalGroup("row3"), LabelText("Weight"), MinValue(0f)]
            public float weight;

            public static ClipVariant Default(AudioClip c) => new ClipVariant
            {
                clip = c,
                volume = 1f,
                loop = false,
                usePitchRandom = false,
                pitchRange = new Vector2(1f, 1f),
                weight = 1f,
            };
        }

        // =========================
        // SFX (World/Gameplay)
        // =========================
        [Serializable]
        public struct SFXEntry
        {
            [HideInInspector] public string Key; // full key ใช้งานจริง

            [FoldoutGroup("@FoldoutLabel", expanded: false)]
            [SerializeField, LabelText("Key")]
            [ValueDropdown("@SoundName.Odin.ShortGroup(\"SFX\")")]
            [ValidateInput(nameof(ValidateShortKey), "Unknown SFX key.", InfoMessageType.Error)]
            private string keyShort;

            // ---- Variants (หลายคลิปต่อคีย์) ----
            [FoldoutGroup("@FoldoutLabel"), TableList(AlwaysExpanded = true)]
            public List<ClipVariant> variants;

            // ---- Random mode ----
            [FoldoutGroup("@FoldoutLabel/Random")]
            public RandomPickMode pickMode;

            [FoldoutGroup("@FoldoutLabel/Random"), Tooltip("กันสุ่มซ้ำตัวเดิมติดกัน (ใช้กับ Random/Weighted)")]
            public bool avoidImmediateRepeat;

            // ---- 3D / Mixer / Cooldown (ระดับ key) ----
            [FoldoutGroup("@FoldoutLabel/3D")] public bool spatial;
            [FoldoutGroup("@FoldoutLabel/3D"), ShowIf(nameof(spatial)), Range(0, 1)] public float spatialBlend;
            [FoldoutGroup("@FoldoutLabel/3D"), ShowIf(nameof(spatial))] public float minDistance;
            [FoldoutGroup("@FoldoutLabel/3D"), ShowIf(nameof(spatial))] public float maxDistance;
            [FoldoutGroup("@FoldoutLabel/3D"), ShowIf(nameof(spatial))] public AudioRolloffMode rolloffMode;

            [FoldoutGroup("@FoldoutLabel/Mixer")] public AudioMixerGroup mixerOverride;

            [FoldoutGroup("@FoldoutLabel/Other"), MinValue(0f)]
            public float cooldown;

            // --- label ที่หัว foldout ---
            private string FoldoutLabel
                => string.IsNullOrEmpty(keyShort) ? "SFX" : $"SFX/{keyShort}";

            // --- validate & sync ---
            private bool ValidateShortKey(string shortName)
            {
                if (string.IsNullOrEmpty(shortName)) return true;
                var full = SoundName.ResolveFullKey("SFX", shortName);
                return SoundName.IsValid(full);
            }

            public void SyncShortToFull()
            {
                if (!string.IsNullOrEmpty(keyShort))
                    Key = SoundName.ResolveFullKey("SFX", keyShort);
            }

            public void SyncFullToShort()
            {
                if (!string.IsNullOrEmpty(Key))
                    keyShort = SoundName.ShortLabel(Key);
            }

            // --- helper เลือก variant ตามโหมดสุ่ม (เก็บ state ภายนอก) ---
            public int PickIndex(ref int lastIndex, ref List<int> shuffleBag, System.Random rnd = null)
            {
                if (variants == null || variants.Count == 0) return -1;
                rnd ??= _rnd;

                switch (pickMode)
                {
                    case RandomPickMode.First:
                        lastIndex = 0; return 0;

                    case RandomPickMode.Sequential:
                        lastIndex = (lastIndex + 1) % variants.Count; return lastIndex;

                    case RandomPickMode.ShuffleNoRepeat:
                        if (shuffleBag == null || shuffleBag.Count == 0)
                        {
                            if (shuffleBag == null) shuffleBag = new List<int>(variants.Count);
                            shuffleBag.Clear();
                            for (int i = 0; i < variants.Count; i++) shuffleBag.Add(i);
                            // shuffle
                            for (int i = 0; i < shuffleBag.Count; i++)
                            {
                                int j = rnd.Next(i, shuffleBag.Count);
                                (shuffleBag[i], shuffleBag[j]) = (shuffleBag[j], shuffleBag[i]);
                            }
                        }
                        int idx = shuffleBag[^1];
                        shuffleBag.RemoveAt(shuffleBag.Count - 1);
                        lastIndex = idx;
                        return idx;

                    case RandomPickMode.WeightedRandom:
                    {
                        float total = 0f;
                        for (int i = 0; i < variants.Count; i++)
                            total += Mathf.Max(0f, variants[i].weight);
                        if (total <= 0f) goto case RandomPickMode.Random;

                        float pick = (float)rnd.NextDouble() * total;
                        float accum = 0f;
                        for (int i = 0; i < variants.Count; i++)
                        {
                            accum += Mathf.Max(0f, variants[i].weight);
                            if (pick <= accum)
                            {
                                if (avoidImmediateRepeat && i == lastIndex && variants.Count > 1)
                                    i = (i + 1) % variants.Count;
                                lastIndex = i;
                                return i;
                            }
                        }
                        lastIndex = variants.Count - 1;
                        return lastIndex;
                    }

                    case RandomPickMode.RandomNoImmediateRepeat:
                    case RandomPickMode.Random:
                    default:
                    {
                        int c = variants.Count;
                        if (c == 1) { lastIndex = 0; return 0; }
                        int i = rnd.Next(0, c);
                        if (avoidImmediateRepeat && i == lastIndex)
                            i = (i + 1) % c;
                        lastIndex = i;
                        return i;
                    }
                }
            }

            private static readonly System.Random _rnd = new System.Random();

            public static SFXEntry Default(string keyFull, AudioClip clipRef)
            {
                return new SFXEntry
                {
                    Key = keyFull,
                    keyShort = SoundName.ShortLabel(keyFull),
                    variants = new List<ClipVariant> { ClipVariant.Default(clipRef) },
                    pickMode = RandomPickMode.Random,
                    avoidImmediateRepeat = true,
                    spatial = false,
                    spatialBlend = 0f,
                    minDistance = 1f,
                    maxDistance = 500f,
                    rolloffMode = AudioRolloffMode.Logarithmic,
                    mixerOverride = null,
                    cooldown = 0.02f,
                };
            }
        }

        // =========================
        // UI (2D/UI sounds)
        // =========================
        [Serializable]
        public struct UIEntry
        {
            [HideInInspector] public string Key;

            [FoldoutGroup("@FoldoutLabel", expanded: false)]
            [SerializeField, LabelText("Key")]
            [ValueDropdown("@SoundName.Odin.ShortGroup(\"UI\")")]
            [ValidateInput(nameof(ValidateShortKey), "Unknown UI key.", InfoMessageType.Error)]
            private string keyShort;

            [FoldoutGroup("@FoldoutLabel"), TableList(AlwaysExpanded = true)]
            public List<ClipVariant> variants;

            [FoldoutGroup("@FoldoutLabel/Random")]
            public RandomPickMode pickMode;

            [FoldoutGroup("@FoldoutLabel/Random"), Tooltip("กันสุ่มซ้ำตัวเดิมติดกัน (ใช้กับ Random/Weighted)")]
            public bool avoidImmediateRepeat;

            [FoldoutGroup("@FoldoutLabel/Mixer")]
            public AudioMixerGroup mixerOverride;

            [FoldoutGroup("@FoldoutLabel/Other"), MinValue(0f)]
            public float cooldown;

            private string FoldoutLabel
                => string.IsNullOrEmpty(keyShort) ? "UI" : $"UI/{keyShort}";

            private bool ValidateShortKey(string shortName)
            {
                if (string.IsNullOrEmpty(shortName)) return true;
                var full = SoundName.ResolveFullKey("UI", shortName);
                return SoundName.IsValid(full);
            }

            public void SyncShortToFull()
            {
                if (!string.IsNullOrEmpty(keyShort))
                    Key = SoundName.ResolveFullKey("UI", keyShort);
            }

            public void SyncFullToShort()
            {
                if (!string.IsNullOrEmpty(Key))
                    keyShort = SoundName.ShortLabel(Key);
            }

            public int PickIndex(ref int lastIndex, ref List<int> shuffleBag, System.Random rnd = null)
            {
                // ใช้ logic เดียวกับ SFXEntry
                if (variants == null || variants.Count == 0) return -1;
                rnd ??= _rnd;
                // reuse algorithm from SFXEntry for brevity
                float total;
                switch (pickMode)
                {
                    case RandomPickMode.First: lastIndex = 0; return 0;
                    case RandomPickMode.Sequential: lastIndex = (lastIndex + 1) % variants.Count; return lastIndex;
                    case RandomPickMode.ShuffleNoRepeat:
                        if (shuffleBag == null || shuffleBag.Count == 0)
                        {
                            shuffleBag ??= new List<int>(variants.Count);
                            shuffleBag.Clear();
                            for (int i = 0; i < variants.Count; i++) shuffleBag.Add(i);
                            for (int i = 0; i < shuffleBag.Count; i++)
                            {
                                int j = rnd.Next(i, shuffleBag.Count);
                                (shuffleBag[i], shuffleBag[j]) = (shuffleBag[j], shuffleBag[i]);
                            }
                        }
                        int idx = shuffleBag[^1];
                        shuffleBag.RemoveAt(shuffleBag.Count - 1);
                        lastIndex = idx; return idx;
                    case RandomPickMode.WeightedRandom:
                        total = 0f;
                        for (int i = 0; i < variants.Count; i++)
                            total += Mathf.Max(0f, variants[i].weight);
                        if (total <= 0f) goto case RandomPickMode.Random;
                        float pick = (float)rnd.NextDouble() * total;
                        float acc = 0f;
                        for (int i = 0; i < variants.Count; i++)
                        {
                            acc += Mathf.Max(0f, variants[i].weight);
                            if (pick <= acc)
                            {
                                if (avoidImmediateRepeat && i == lastIndex && variants.Count > 1)
                                    i = (i + 1) % variants.Count;
                                lastIndex = i;
                                return i;
                            }
                        }
                        lastIndex = variants.Count - 1; return lastIndex;
                    case RandomPickMode.RandomNoImmediateRepeat:
                    case RandomPickMode.Random:
                    default:
                        int c = variants.Count;
                        if (c == 1) { lastIndex = 0; return 0; }
                        int r = rnd.Next(0, c);
                        if (avoidImmediateRepeat && r == lastIndex) r = (r + 1) % c;
                        lastIndex = r; return r;
                }
            }
            private static readonly System.Random _rnd = new System.Random();

            public static UIEntry Default(string keyFull, AudioClip clipRef) => new UIEntry
            {
                Key = keyFull,
                keyShort = SoundName.ShortLabel(keyFull),
                variants = new List<ClipVariant> { ClipVariant.Default(clipRef) },
                pickMode = RandomPickMode.Random,
                avoidImmediateRepeat = true,
                mixerOverride = null,
                cooldown = 0.02f,
            };
        }

        // =========================
        // BGM
        // =========================
        [Serializable]
        public struct BGMEntry
        {
            [HideInInspector] public string Key;

            [FoldoutGroup("@keyShort", expanded: false)]
            [LabelText("Key"), SerializeField]
            [ValueDropdown("@SoundName.Odin.ShortGroup(\"BGM\")")]
            [ValidateInput(nameof(ValidateShortKey), "Unknown BGM key.", InfoMessageType.Error)]
            private string keyShort;

            [FoldoutGroup("@keyShort/Clip")] public AudioClip clip;
            [FoldoutGroup("@keyShort/Clip"), Range(0f, 1f)] public float volume;
            [FoldoutGroup("@keyShort/Mixer")] public AudioMixerGroup mixerOverride;

            private bool ValidateShortKey(string shortName)
            {
                if (string.IsNullOrEmpty(shortName)) return true;
                var full = SoundName.ResolveFullKey("BGM", shortName);
                return SoundName.IsValid(full);
            }

            public void SyncShortToFull()
            {
                if (!string.IsNullOrEmpty(keyShort))
                    Key = SoundName.ResolveFullKey("BGM", keyShort);
            }

            public void SyncFullToShort()
            {
                if (!string.IsNullOrEmpty(Key))
                    keyShort = SoundName.ShortLabel(Key);
            }

            public static BGMEntry Default(string keyFull, AudioClip clipRef) => new BGMEntry
            {
                Key = keyFull,
                keyShort = SoundName.ShortLabel(keyFull),
                clip = clipRef,
                volume = 1f,
                mixerOverride = null,
            };
        }

        // =========================
        // Lists
        // =========================
        [Title("SFX (Gameplay/World)")]
        [TableList(AlwaysExpanded = true, NumberOfItemsPerPage = 12)]
        public List<SFXEntry> sfx = new();

        [Title("UI (2D/UI sounds)")]
        [TableList(AlwaysExpanded = true, NumberOfItemsPerPage = 12)]
        public List<UIEntry> ui = new();

        [Title("BGM")]
        [TableList(AlwaysExpanded = true, NumberOfItemsPerPage = 12)]
        public List<BGMEntry> bgm = new();

        // Build-time dictionaries
        [NonSerialized] public Dictionary<string, SFXEntry> SfxMap;
        [NonSerialized] public Dictionary<string, UIEntry>  UiMap;
        [NonSerialized] public Dictionary<string, BGMEntry> BgmMap;

        private void OnEnable() => Rebuild();

#if UNITY_EDITOR
        private void OnValidate()
        {
            for (int i = 0; i < sfx.Count; i++)
            {
                var e = sfx[i];
                if (!string.IsNullOrEmpty(e.Key)) e.SyncFullToShort();
                e.SyncShortToFull();
                sfx[i] = e;
            }

            for (int i = 0; i < ui.Count; i++)
            {
                var e = ui[i];
                if (!string.IsNullOrEmpty(e.Key)) e.SyncFullToShort();
                e.SyncShortToFull();
                ui[i] = e;
            }

            for (int i = 0; i < bgm.Count; i++)
            {
                var e = bgm[i];
                if (!string.IsNullOrEmpty(e.Key)) e.SyncFullToShort();
                e.SyncShortToFull();
                bgm[i] = e;
            }

            Rebuild();
        }
#endif

        public void Rebuild()
        {
            SfxMap = new Dictionary<string, SFXEntry>(StringComparer.Ordinal);
            foreach (var e in sfx)
            {
                if (string.IsNullOrEmpty(e.Key)) continue;
                if (e.variants == null || e.variants.Count == 0) continue;
                bool anyClip = false;
                foreach (var v in e.variants) if (v.clip) { anyClip = true; break; }
                if (!anyClip) continue;
                if (!SoundName.IsValid(e.Key)) continue;
                if (!SfxMap.ContainsKey(e.Key)) SfxMap.Add(e.Key, e);
            }

            UiMap = new Dictionary<string, UIEntry>(StringComparer.Ordinal);
            foreach (var e in ui)
            {
                if (string.IsNullOrEmpty(e.Key)) continue;
                if (e.variants == null || e.variants.Count == 0) continue;
                bool anyClip = false;
                foreach (var v in e.variants) if (v.clip) { anyClip = true; break; }
                if (!anyClip) continue;
                if (!SoundName.IsValid(e.Key)) continue;
                if (!UiMap.ContainsKey(e.Key)) UiMap.Add(e.Key, e);
            }

            BgmMap = new Dictionary<string, BGMEntry>(StringComparer.Ordinal);
            foreach (var e in bgm)
            {
                if (string.IsNullOrEmpty(e.Key)) continue;
                if (e.clip == null) continue;
                if (!SoundName.IsValid(e.Key)) continue;
                if (!BgmMap.ContainsKey(e.Key)) BgmMap.Add(e.Key, e);
            }
        }
    }
}
