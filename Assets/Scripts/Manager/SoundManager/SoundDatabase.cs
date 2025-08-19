// SoundDatabase.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Manager.SoundManager
{
    [CreateAssetMenu(fileName = "SoundDatabase", menuName = "Audio/Sound Database", order = 0)]
    [HideReferenceObjectPicker, Searchable]
    public class SoundDatabase : SerializedScriptableObject
    {
        // ========================= Common =========================
        public enum RandomPickMode
        {
            First = 0,
            Random = 1,
            WeightedRandom = 2,
            Sequential = 3,
            ShuffleNoRepeat = 4,
            RandomNoImmediateRepeat = 5,
        }

        [Serializable, InlineProperty, HideLabel]
        public struct ClipVariant
        {
            [LabelText("Clip"), PropertyOrder(-10)]
            public AudioClip clip;

            [LabelText("Vol"), PropertyOrder(-9), Range(0f, 1f)]
            public float volume;

            [LabelText("Loop"), PropertyOrder(-8)]
            public bool loop;

            [LabelText("Rnd Pitch"), PropertyOrder(-7)]
            public bool usePitchRandom;

            [LabelText("Pitch"), PropertyOrder(-6), ShowIf(nameof(usePitchRandom))]
            public Vector2 pitchRange;

            [LabelText("Weight"), PropertyOrder(-5), MinValue(0f)]
            public float weight;
        }

        // ========================= SFX =========================
        [Serializable]
        public struct SFXEntry
        {
            [HideInInspector] public string Key;

            private string FoldoutLabel => string.IsNullOrEmpty(keyShort) ? "SFX" : $"{keyShort}";

            [FoldoutGroup("$FoldoutLabel")]
            [SerializeField, LabelText("Key")]
            [ValueDropdown("@SoundName.Odin.ShortGroup(\"SFX\")")]
            [ValidateInput(nameof(ValidateShortKey), "Unknown SFX key.", InfoMessageType.Error)]
            private string keyShort;

            [FoldoutGroup("$FoldoutLabel")]
            [LabelText("Variants")]
            [ValidateInput(nameof(ValidateVariantsHasClip), "Variants must contain at least one AudioClip.", InfoMessageType.Error)]
            [ListDrawerSettings(
                Expanded = true,
                DraggableItems = true,
                ShowIndexLabels = true,
                OnBeginListElementGUI = "__BeginVariantRow_Zebra",
                OnEndListElementGUI = "__EndVariantRow_Zebra"
            )]
            public List<ClipVariant> variants;

            [FoldoutGroup("$FoldoutLabel"), LabelText("Pick Mode")]
            public RandomPickMode pickMode;

            [FoldoutGroup("$FoldoutLabel"), LabelText("Avoid Immediate Repeat")]
            [ShowIf("@pickMode == RandomPickMode.Random || pickMode == RandomPickMode.WeightedRandom")]
            public bool avoidImmediateRepeat;

            [FoldoutGroup("$FoldoutLabel"), LabelText("Spatial")] public bool spatial;
            [FoldoutGroup("$FoldoutLabel"), ShowIf(nameof(spatial)), Range(0, 1), LabelText("Spatial Blend")] public float spatialBlend;
            [FoldoutGroup("$FoldoutLabel"), ShowIf(nameof(spatial)), LabelText("Min Dist")] public float minDistance;
            [FoldoutGroup("$FoldoutLabel"), ShowIf(nameof(spatial)), LabelText("Max Dist")] public float maxDistance;
            [FoldoutGroup("$FoldoutLabel"), ShowIf(nameof(spatial)), LabelText("Rolloff")] public AudioRolloffMode rolloffMode;

            [FoldoutGroup("$FoldoutLabel"), LabelText("Mixer Override")] public AudioMixerGroup mixerOverride;

            [FoldoutGroup("$FoldoutLabel"), LabelText("Cooldown"), MinValue(0f)]
            public float cooldown;

            // ---- Validation ----
            private bool ValidateShortKey(string shortName)
            {
                if (string.IsNullOrEmpty(shortName)) return true;
                var full = SoundName.ResolveFullKey("SFX", shortName);
                return SoundName.IsValid(full);
            }

            private bool ValidateVariantsHasClip(List<ClipVariant> list)
                => list != null && list.Any(v => v.clip != null);

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

#if UNITY_EDITOR
            [NonSerialized] private int __rowDepthGuard;

            private void __BeginVariantRow_Zebra(int index)
            {
                if (__rowDepthGuard++ > 0) return;
                var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(0f), GUILayout.ExpandWidth(true));
                var col = (index % 2 == 0)
                    ? new Color(1f, 1f, 1f, 0.06f)
                    : new Color(1f, 1f, 1f, 0.12f);
                EditorGUI.DrawRect(rect, col);
            }

            private void __EndVariantRow_Zebra(int index)
            {
                if (--__rowDepthGuard > 0) return;
                GUILayout.Space(2f);
            }
#endif
        }

        // ========================= UI =========================
        [Serializable]
        public struct UIEntry
        {
            [HideInInspector] public string Key;

            private string FoldoutLabel => string.IsNullOrEmpty(keyShort) ? "UI" : $"{keyShort}";

            [FoldoutGroup("$FoldoutLabel")]
            [SerializeField, LabelText("Key")]
            [ValueDropdown("@SoundName.Odin.ShortGroup(\"UI\")")]
            [ValidateInput(nameof(ValidateShortKey), "Unknown UI key.", InfoMessageType.Error)]
            private string keyShort;

            [FoldoutGroup("$FoldoutLabel")]
            [LabelText("Variants")]
            [ValidateInput(nameof(ValidateVariantsHasClip), "Variants must contain at least one AudioClip.", InfoMessageType.Error)]
            [ListDrawerSettings(
                Expanded = true,
                DraggableItems = true,
                ShowIndexLabels = true,
                OnBeginListElementGUI = "__BeginVariantRow_Zebra",
                OnEndListElementGUI = "__EndVariantRow_Zebra"
            )]
            public List<ClipVariant> variants;

            [FoldoutGroup("$FoldoutLabel"), LabelText("Pick Mode")]
            public RandomPickMode pickMode;

            [FoldoutGroup("$FoldoutLabel"), LabelText("Avoid Immediate Repeat")]
            [ShowIf("@pickMode == RandomPickMode.Random || pickMode == RandomPickMode.WeightedRandom")]
            public bool avoidImmediateRepeat;

            [FoldoutGroup("$FoldoutLabel"), LabelText("Mixer Override")]
            public AudioMixerGroup mixerOverride;

            [FoldoutGroup("$FoldoutLabel"), LabelText("Cooldown"), MinValue(0f)]
            public float cooldown;

            private bool ValidateShortKey(string shortName)
            {
                if (string.IsNullOrEmpty(shortName)) return true;
                var full = SoundName.ResolveFullKey("UI", shortName);
                return SoundName.IsValid(full);
            }

            private bool ValidateVariantsHasClip(List<ClipVariant> list)
                => list != null && list.Any(v => v.clip != null);

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

#if UNITY_EDITOR
            [NonSerialized] private int __rowDepthGuard;
            private void __BeginVariantRow_Zebra(int index)
            {
                if (__rowDepthGuard++ > 0) return;
                var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(0f), GUILayout.ExpandWidth(true));
                var col = (index % 2 == 0)
                    ? new Color(1f, 1f, 1f, 0.06f)
                    : new Color(1f, 1f, 1f, 0.12f);
                EditorGUI.DrawRect(rect, col);
            }
            private void __EndVariantRow_Zebra(int index)
            {
                if (--__rowDepthGuard > 0) return;
                GUILayout.Space(2f);
            }
#endif
        }

        // ========================= BGM =========================
        [Serializable]
        public struct BGMEntry
        {
            [HideInInspector] public string Key;

            private string FoldoutLabel => string.IsNullOrEmpty(keyShort) ? "BGM" : $"{keyShort}";

            [FoldoutGroup("$FoldoutLabel"), LabelText("Key"), SerializeField]
            [ValueDropdown("@SoundName.Odin.ShortGroup(\"BGM\")")]
            [ValidateInput(nameof(ValidateShortKey), "Unknown BGM key.", InfoMessageType.Error)]
            private string keyShort;

            [FoldoutGroup("$FoldoutLabel"), LabelText("Clip")]
            [ValidateInput(nameof(ValidateClipNotNull), "BGM must have a clip.", InfoMessageType.Error)]
            public AudioClip clip;

            [FoldoutGroup("$FoldoutLabel"), LabelText("Volume"), Range(0f, 1f)] public float volume;

            [FoldoutGroup("$FoldoutLabel"), LabelText("Mixer Override")] public AudioMixerGroup mixerOverride;

            private bool ValidateShortKey(string shortName)
            {
                if (string.IsNullOrEmpty(shortName)) return true;
                var full = SoundName.ResolveFullKey("BGM", shortName);
                return SoundName.IsValid(full);
            }

            private bool ValidateClipNotNull(AudioClip c) => c != null;

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
        }

        // ========================= Tabs (แนวตั้ง) =========================
        [TabGroup("DB", "SFX")]
        [ValidateInput(nameof(ValidateSfxList), "Duplicate/invalid keys in SFX, or entries without clips.", InfoMessageType.Error)]
        [ListDrawerSettings(Expanded = true, DraggableItems = true, NumberOfItemsPerPage = 60)]
        public List<SFXEntry> sfx = new();

        [TabGroup("DB", "UI")]
        [ValidateInput(nameof(ValidateUiList), "Duplicate/invalid keys in UI, or entries without clips.", InfoMessageType.Error)]
        [ListDrawerSettings(Expanded = true, DraggableItems = true, NumberOfItemsPerPage = 60)]
        public List<UIEntry> ui = new();

        [TabGroup("DB", "BGM")]
        [ValidateInput(nameof(ValidateBgmList), "Duplicate/invalid keys in BGM, or entries without clips.", InfoMessageType.Error)]
        [ListDrawerSettings(Expanded = true, DraggableItems = true, NumberOfItemsPerPage = 60)]
        public List<BGMEntry> bgm = new();

        // ========================= Overview (ล่างสุด) =========================
        [PropertyOrder(99999)]
        [BoxGroup("Overview", false)]
        [ShowInInspector, ReadOnly, LabelText("Counts")]
        private string __Counts => $"SFX {sfx?.Count ?? 0} • UI {ui?.Count ?? 0} • BGM {bgm?.Count ?? 0}";

        [PropertyOrder(99999)]
        [ButtonGroup("Overview/Buttons")]
        [Button("Sort Keys (SoundName Order)", ButtonSizes.Medium)]
        private void __BtnSort()
        {
            SortBySoundNameOrder();
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        [PropertyOrder(99999)]
        [ButtonGroup("Overview/Buttons")]
        [Button("Populate Keys", ButtonSizes.Medium)]
        private void __BtnPopulate()
        {
            PopulateKeys();
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        // ========================= Validation for lists =========================
        private bool ValidateSfxList(List<SFXEntry> list)
        {
            if (list == null) return true;
            var keys = new HashSet<string>();
            foreach (var e in list)
            {
                if (string.IsNullOrEmpty(e.Key) || !SoundName.IsValid(e.Key)) return false;
                if (!keys.Add(e.Key)) return false;
                if (e.variants == null || !e.variants.Any(v => v.clip != null)) return false;
            }
            return true;
        }

        private bool ValidateUiList(List<UIEntry> list)
        {
            if (list == null) return true;
            var keys = new HashSet<string>();
            foreach (var e in list)
            {
                if (string.IsNullOrEmpty(e.Key) || !SoundName.IsValid(e.Key)) return false;
                if (!keys.Add(e.Key)) return false;
                if (e.variants == null || !e.variants.Any(v => v.clip != null)) return false;
            }
            return true;
        }

        private bool ValidateBgmList(List<BGMEntry> list)
        {
            if (list == null) return true;
            var keys = new HashSet<string>();
            foreach (var e in list)
            {
                if (string.IsNullOrEmpty(e.Key) || !SoundName.IsValid(e.Key)) return false;
                if (!keys.Add(e.Key)) return false;
                if (e.clip == null) return false;
            }
            return true;
        }

        // ========================= Populate & Sort =========================
        private void PopulateKeys()
        {
            // 1) ซิงค์รายการเดิมให้ Key ถูกต้องก่อน (ใช้ keyShort -> Key ถ้าว่าง)
            SyncExistingEntriesShortToFull();

            // 2) เติมรายการที่ขาด พร้อมค่าเริ่มต้น
            AddMissingSfxKeys();
            AddMissingUiKeys();
            AddMissingBgmKeys();

            // 3) เรียงลำดับตาม SoundName
            SortBySoundNameOrder();
        }

        private void SyncExistingEntriesShortToFull()
        {
            // SFX
            if (sfx != null)
            {
                for (int i = 0; i < sfx.Count; i++)
                {
                    var e = sfx[i];
                    if (string.IsNullOrEmpty(e.Key))
                        e.SyncShortToFull();
                    e.SyncFullToShort();
                    sfx[i] = e; // struct: write back
                }
            }

            // UI
            if (ui != null)
            {
                for (int i = 0; i < ui.Count; i++)
                {
                    var e = ui[i];
                    if (string.IsNullOrEmpty(e.Key))
                        e.SyncShortToFull();
                    e.SyncFullToShort();
                    ui[i] = e;
                }
            }

            // BGM
            if (bgm != null)
            {
                for (int i = 0; i < bgm.Count; i++)
                {
                    var e = bgm[i];
                    if (string.IsNullOrEmpty(e.Key))
                        e.SyncShortToFull();
                    e.SyncFullToShort();
                    bgm[i] = e;
                }
            }
        }

        private void AddMissingSfxKeys()
        {
            var existing = new HashSet<string>(sfx.Where(e => !string.IsNullOrEmpty(e.Key)).Select(e => e.Key), StringComparer.Ordinal);

            foreach (var key in SoundName.OrderedKeysOf("SFX"))
            {
                if (existing.Contains(key)) continue;

                var e = new SFXEntry
                {
                    Key = key,
                    variants = new List<ClipVariant>(1)
                    {
                        new ClipVariant
                        {
                            clip = null,
                            volume = 1f,
                            loop = false,
                            usePitchRandom = true,
                            pitchRange = new Vector2(0.85f, 1.15f),
                            weight = 1f
                        }
                    }
                };
                e.SyncFullToShort();
                sfx.Add(e);
            }
        }

        private void AddMissingUiKeys()
        {
            var existing = new HashSet<string>(ui.Where(e => !string.IsNullOrEmpty(e.Key)).Select(e => e.Key), StringComparer.Ordinal);

            foreach (var key in SoundName.OrderedKeysOf("UI"))
            {
                if (existing.Contains(key)) continue;

                var e = new UIEntry
                {
                    Key = key,
                    variants = new List<ClipVariant>(1)
                    {
                        new ClipVariant
                        {
                            clip = null,
                            volume = 1f,
                            loop = false,
                            usePitchRandom = true,
                            pitchRange = new Vector2(0.85f, 1.15f),
                            weight = 1f
                        }
                    }
                };
                e.SyncFullToShort();
                ui.Add(e);
            }
        }

        private void AddMissingBgmKeys()
        {
            var existing = new HashSet<string>(bgm.Where(e => !string.IsNullOrEmpty(e.Key)).Select(e => e.Key), StringComparer.Ordinal);

            foreach (var key in SoundName.OrderedKeysOf("BGM"))
            {
                if (existing.Contains(key)) continue;

                var e = new BGMEntry
                {
                    Key = key,
                    volume = 1f
                };
                e.SyncFullToShort();
                bgm.Add(e);
            }
        }

        private void SortBySoundNameOrder()
        {
            var sfxOrder = SoundName.OrderedKeysOf("SFX").Select((k, i) => (k, i)).ToDictionary(x => x.k, x => x.i, StringComparer.Ordinal);
            var uiOrder  = SoundName.OrderedKeysOf("UI") .Select((k, i) => (k, i)).ToDictionary(x => x.k, x => x.i, StringComparer.Ordinal);
            var bgmOrder = SoundName.OrderedKeysOf("BGM").Select((k, i) => (k, i)).ToDictionary(x => x.k, x => x.i, StringComparer.Ordinal);

            if (sfx != null) sfx.Sort((a, b) => GetOrder(sfxOrder, a.Key).CompareTo(GetOrder(sfxOrder, b.Key)));
            if (ui  != null) ui .Sort((a, b) => GetOrder(uiOrder,  a.Key).CompareTo(GetOrder(uiOrder,  b.Key)));
            if (bgm != null) bgm.Sort((a, b) => GetOrder(bgmOrder, a.Key).CompareTo(GetOrder(bgmOrder, b.Key)));

            static int GetOrder(Dictionary<string,int> map, string key)
                => (key != null && map.TryGetValue(key, out var idx)) ? idx : int.MaxValue;
        }

        // ========================= Default seeding when adding by hand =========================
        private void OnValidate()
        {
            // SFX
            if (sfx != null)
            {
                for (int i = 0; i < sfx.Count; i++)
                {
                    var e = sfx[i];
                    e.SyncShortToFull();
                    e.SyncFullToShort();

                    if (e.variants == null || e.variants.Count == 0)
                    {
                        e.variants = new List<ClipVariant>(1)
                        {
                            new ClipVariant
                            {
                                clip = null,
                                volume = 1f,
                                loop = false,
                                usePitchRandom = true,
                                pitchRange = new Vector2(0.85f, 1.15f),
                                weight = 1f
                            }
                        };
                    }
                    sfx[i] = e;
                }
            }

            // UI
            if (ui != null)
            {
                for (int i = 0; i < ui.Count; i++)
                {
                    var e = ui[i];
                    e.SyncShortToFull();
                    e.SyncFullToShort();

                    if (e.variants == null || e.variants.Count == 0)
                    {
                        e.variants = new List<ClipVariant>(1)
                        {
                            new ClipVariant
                            {
                                clip = null,
                                volume = 1f,
                                loop = false,
                                usePitchRandom = true,
                                pitchRange = new Vector2(0.85f, 1.15f),
                                weight = 1f
                            }
                        };
                    }
                    ui[i] = e;
                }
            }

            // BGM
            if (bgm != null)
            {
                for (int i = 0; i < bgm.Count; i++)
                {
                    var e = bgm[i];
                    e.SyncShortToFull();
                    e.SyncFullToShort();

                    // ตั้งค่าเริ่มต้นเฉพาะกรณีที่ยังไม่ถูกตั้ง (ค่า default Unity = 0)
                    if (Mathf.Approximately(e.volume, 0f))
                        e.volume = 1f;

                    bgm[i] = e;
                }
            }
        }
    }
}
