using System;
using System.Collections.Generic;
using System.Linq;
using Characters.Controllers;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.FeedbackSystems
{
    public class FeedbackSystem : SerializedMonoBehaviour
    {
        [Serializable]
        public struct FeedbackEntry
        {
            // เก็บค่าจริง (path เต็ม) ไว้หลังฉาก
            [HideInInspector] public string Key;

            // ====== KEY UI (อยู่ซ้าย) ======
            [TableColumnWidth(150, Resizable = true)]
            [PropertyOrder(-10)] // ลำดับน้อยกว่า -> ไปอยู่ซ้ายสุด
            [HideLabel]
            [ShowInInspector]
            [ValueDropdown("@Characters.FeedbackSystems.FeedbackName.Odin.ShortGroup(GroupFilter)")]
            [ValidateInput("ValidateKnownKeyUI", "Unknown key (not defined).", InfoMessageType.Error)]
            public string KeyUI
            {
                get => FeedbackName.ShortLabel(Key);
                set => Key = FeedbackName.ResolveFullKey(GroupFilter, value);
            }

            // ====== PLAYER (อยู่ถัดมา) ======
            [TableColumnWidth(280, Resizable = true)]
            [PropertyOrder(-5)]
            [LabelText("Player")]
            [ValidateInput("ValidatePlayerAssigned", "Assign MMF_Player for this key.", InfoMessageType.Warning)]
            public MMF_Player Player;

#if UNITY_EDITOR
            // ปุ่มเทสต่อแถว (ไปขวาสุด)
            [TableColumnWidth(60), PropertyOrder(10)]
            [GUIColor(0.35f, 0.85f, 0.35f)]
            [Button("Play")]
            private void __Play()
            {
                Player?.PlayFeedbacks();
            }

            [TableColumnWidth(60), PropertyOrder(11)]
            [GUIColor(0.9f, 0.4f, 0.4f)]
            [Button("Stop")]
            private void __Stop()
            {
                Player?.StopFeedbacks();
            }
#endif

            [HideInInspector] public string GroupFilter;

            // ===== Validators =====
            private bool ValidateKnownKeyUI(string shortName)
            {
                if (string.IsNullOrEmpty(shortName)) return true;
                var full = FeedbackName.ResolveFullKey(GroupFilter, shortName);
                return FeedbackName.IsValid(full);
            }

            private bool ValidatePlayerAssigned(MMF_Player player)
                => string.IsNullOrEmpty(Key) || player != null;
        }


        // ===== UI: แยกแท็บ Character / Skill =====

        [TabGroup("Feedback", "Character")]
        [PropertyOrder(0)]
        [Searchable]
        [TableList(AlwaysExpanded = true, ShowIndexLabels = false)]
        [ListDrawerSettings(ShowItemCount = true, DraggableItems = true, ShowPaging = true, NumberOfItemsPerPage = 12)]
        [SerializeField]
        private List<FeedbackEntry> characterEntries = new();

        [TabGroup("Feedback", "Skill")]
        [PropertyOrder(0)]
        [Searchable]
        [TableList(AlwaysExpanded = true, ShowIndexLabels = false)]
        [ListDrawerSettings(ShowItemCount = true, DraggableItems = true, ShowPaging = true, NumberOfItemsPerPage = 12)]
        [SerializeField]
        private List<FeedbackEntry> skillEntries = new();

        // ไว้ใช้จริงตอนรัน (รวมสองลิสต์)
        protected Dictionary<string, MMF_Player> feedbackMap;

        [FoldoutGroup("Trail")] [SerializeField]
        private TrailRenderer trail;

        protected readonly HashSet<string> ignoreFeedbackList = new();
        private BaseController _owner;

        public virtual void AssignData(BaseController owner)
        {
            _owner = owner;
            RebuildMap();
        }

        private void Awake()
        {
            EnsureGroupFilters();
            RebuildMap();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureGroupFilters();
            RebuildMap();
        }
#endif

        private void EnsureGroupFilters()
        {
            for (int i = 0; i < characterEntries.Count; i++)
            {
                var e = characterEntries[i];
                e.GroupFilter = "Character";
                characterEntries[i] = e;
            }

            for (int i = 0; i < skillEntries.Count; i++)
            {
                var e = skillEntries[i];
                e.GroupFilter = "Skill";
                skillEntries[i] = e;
            }
        }

        private void RebuildMap()
        {
            feedbackMap ??= new Dictionary<string, MMF_Player>(StringComparer.Ordinal);
            feedbackMap.Clear();

            void AddRange(List<FeedbackEntry> list)
            {
                if (list == null) return;
                foreach (var e in list)
                {
                    if (string.IsNullOrEmpty(e.Key)) continue;
                    if (feedbackMap.ContainsKey(e.Key)) continue; // กันซ้ำ
                    feedbackMap.Add(e.Key, e.Player);
                }
            }

            AddRange(characterEntries);
            AddRange(skillEntries);
        }

        // ===== Runtime API =====

        public virtual void PlayFeedback(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (ignoreFeedbackList.Contains(key)) return;
            if (feedbackMap != null && feedbackMap.TryGetValue(key, out var p) && p != null) p.PlayFeedbacks();
        }

        public virtual void StopFeedback(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (feedbackMap != null && feedbackMap.TryGetValue(key, out var p) && p != null) p.StopFeedbacks();
        }

        public virtual bool IsFeedbackPlaying(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            return feedbackMap != null && feedbackMap.TryGetValue(key, out var p) && p != null && p.IsPlaying;
        }

        public virtual void SetIgnoreFeedback(string key, bool ignore)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (ignore) ignoreFeedbackList.Add(key);
            else ignoreFeedbackList.Remove(key);
        }

        public MMF_Player GetFeedback(string key)
            => (feedbackMap != null && feedbackMap.TryGetValue(key, out var p)) ? p : null;

        public void ShowTrail(bool enable)
        {
            if (!trail) return;
            trail.Clear();
            trail.ResetBounds();
            trail.ResetLocalBounds();
            trail.emitting = enable;
        }

        public void ResetFeedbackSystem()
        {
            if (!trail) return;
            trail.Clear();
            trail.ResetBounds();
            trail.ResetLocalBounds();
        }

        // ===== Editor Utilities =====
#if UNITY_EDITOR
        [TabGroup("Feedback", "Character"), PropertyOrder(1)]
        [HorizontalGroup("Feedback/Character/Buttons")]
        [Button("Populate Keys"), GUIColor(0.25f, 0.7f, 1f)]
        private void PopulateCharacterKeys()
        {
            var set = new HashSet<string>(characterEntries.Where(e => !string.IsNullOrEmpty(e.Key)).Select(e => e.Key));
            foreach (var key in FeedbackName.OrderedKeysOf("Character"))
                if (!set.Contains(key))
                    characterEntries.Add(new FeedbackEntry { Key = key, GroupFilter = "Character" });
            RebuildMap();
        }

        [TabGroup("Feedback", "Character"), PropertyOrder(1)]
        [HorizontalGroup("Feedback/Character/Buttons")]
        [Button("Sort by Declaration"), GUIColor(0.8f, 0.8f, 1f)]
        private void SortCharacterByDeclaration()
            => SortByDeclaration(characterEntries, FeedbackName.OrderedKeysOf("Character"));

        // ------------------------

        [TabGroup("Feedback", "Skill"), PropertyOrder(1)]
        [HorizontalGroup("Feedback/Skill/Buttons")]
        [Button("Populate Keys"), GUIColor(0.25f, 0.7f, 1f)]
        private void PopulateSkillKeys()
        {
            var set = new HashSet<string>(skillEntries.Where(e => !string.IsNullOrEmpty(e.Key)).Select(e => e.Key));
            foreach (var key in FeedbackName.OrderedKeysOf("Skill"))
                if (!set.Contains(key))
                    skillEntries.Add(new FeedbackEntry { Key = key, GroupFilter = "Skill" });
            RebuildMap();
        }

        [TabGroup("Feedback", "Skill"), PropertyOrder(1)]
        [HorizontalGroup("Feedback/Skill/Buttons")]
        [Button("Sort by Declaration"), GUIColor(0.8f, 0.8f, 1f)]
        private void SortSkillByDeclaration()
            => SortByDeclaration(skillEntries, FeedbackName.OrderedKeysOf("Skill"));

        // ===== helpers for editor buttons =====
        private static void SortByDeclaration(List<FeedbackEntry> list, IReadOnlyList<string> ordered)
        {
            var index = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < ordered.Count; i++) index[ordered[i]] = i;

            list.Sort((a, b) =>
            {
                int ia = int.MaxValue; // init กันคอมไพเลอร์
                int ib = int.MaxValue;

                bool hasA = !string.IsNullOrEmpty(a.Key) && index.TryGetValue(a.Key, out ia);
                bool hasB = !string.IsNullOrEmpty(b.Key) && index.TryGetValue(b.Key, out ib);

                if (hasA && hasB) return ia.CompareTo(ib); // ทั้งคู่รู้จัก → ตามลำดับประกาศ
                if (hasA) return -1; // รู้จักมาก่อน
                if (hasB) return 1; // อีกรู้จักมาก่อน

                // ทั้งคู่ไม่รู้จัก → เรียงตามอักษรให้ deterministic
                string ak = a.Key ?? string.Empty;
                string bk = b.Key ?? string.Empty;
                return string.CompareOrdinal(ak, bk);
            });
        }
#endif
    }
}