// SoundName.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;

namespace Manager.SoundManager
{
    /// <summary>
    /// คีย์เสียง + ตัวช่วย Odin Dropdown (SFX / UI / BGM)
    /// </summary>
    public static class SoundName
    {
        public static class SFX
        {
            public const string SwordSlash = "SFX/SwordSlash";
            public const string SwordHit   = "SFX/SwordHit";
            public const string Parry      = "SFX/Parry";
            public const string Dash       = "SFX/Dash";
            public const string LevelUp    = "SFX/LevelUp";
            public const string Collect    = "SFX/Collect";
            public const string Hurt       = "SFX/Hurt";
            public const string Death      = "SFX/Death";
            public const string Button     = "SFX/Button";
        }

        public static class UI
        {
            public const string Click        = "UI/Click";
            public const string Hover        = "UI/Hover";
            public const string Confirm      = "UI/Confirm";
            public const string Cancel       = "UI/Cancel";
            public const string OpenWindow   = "UI/OpenWindow";
            public const string CloseWindow  = "UI/CloseWindow";
            public const string ToggleOn     = "UI/ToggleOn";
            public const string ToggleOff    = "UI/ToggleOff";
            public const string Notification = "UI/Notification";
            public const string Reward       = "UI/Reward";
            public const string Error        = "UI/Error";
        }

        public static class BGM
        {
            public const string Stage_1 = "BGM/Stage_1";
            public const string Stage_2 = "BGM/Stage_2";
            public const string Boss    = "BGM/Boss";
            public const string Menu    = "BGM/Menu";
        }

        // ---------- Cache ----------
        private static IReadOnlyList<string> _allKeys;
        private static HashSet<string> _set;
        private static Dictionary<string, List<string>> _orderedByGroup;

        public static IReadOnlyList<string> AllKeys => _allKeys ??= BuildAllKeysOrdered();

        public static bool IsValid(string key)
        {
            _set ??= new HashSet<string>(AllKeys);
            return _set.Contains(key);
        }

        public static IReadOnlyList<string> OrderedKeysOf(string group)
        {
            EnsureCaches();
            return _orderedByGroup != null && _orderedByGroup.TryGetValue(group, out var list)
                ? list : Array.Empty<string>();
        }

        public static string ShortLabel(string full)
        {
            if (string.IsNullOrEmpty(full)) return "";
            int i = full.LastIndexOf('/');
            return i >= 0 ? full[(i + 1)..] : full;
        }

        public static string ResolveFullKey(string group, string shortName)
        {
            foreach (var e in EnumerateEntriesOrdered())
                if (e.Group == group && ShortLabel(e.Value) == shortName)
                    return e.Value;
            return shortName;
        }

#if UNITY_EDITOR
        public static class Odin
        {
            public static IEnumerable<ValueDropdownItem<string>> ShortGroup(string group)
            {
                foreach (var e in EnumerateEntriesOrdered())
                {
                    if (e.Group != group) continue;
                    var label = ShortLabel(e.Value);
                    yield return new ValueDropdownItem<string>(label, label);
                }
            }

            public static IEnumerable<ValueDropdownItem<string>> ShortGroupWithNone(string group)
            {
                yield return new ValueDropdownItem<string>("<None>", "");
                foreach (var e in EnumerateEntriesOrdered())
                {
                    if (e.Group != group) continue;
                    var label = ShortLabel(e.Value);
                    yield return new ValueDropdownItem<string>(label, label);
                }
            }
        }
#endif

        // ---------- internals ----------
        private struct Entry { public string Group; public string Name; public string Value; }

        private static void EnsureCaches()
        {
            if (_orderedByGroup == null || _allKeys == null)
                _allKeys = BuildAllKeysOrdered();
        }

        private static IReadOnlyList<string> BuildAllKeysOrdered()
        {
            _orderedByGroup = new Dictionary<string, List<string>>();
            var list = new List<string>(96);
            foreach (var e in EnumerateEntriesOrdered())
            {
                list.Add(e.Value);
                if (!_orderedByGroup.TryGetValue(e.Group, out var g))
                {
                    g = new List<string>();
                    _orderedByGroup[e.Group] = g;
                }
                g.Add(e.Value);
            }
            return list;
        }

        private static IEnumerable<Entry> EnumerateEntriesOrdered()
        {
            var groups = typeof(SoundName)
                .GetNestedTypes(BindingFlags.Public)
                .Where(t => t.IsClass)
                .OrderBy(t => t.MetadataToken);

            foreach (var g in groups)
            {
                string groupName = g.Name;
                var fields = g.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                    .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                    .OrderBy(f => f.MetadataToken);

                foreach (var f in fields)
                {
                    string value = (string)f.GetRawConstantValue();
                    yield return new Entry { Group = groupName, Name = f.Name, Value = value };
                }
            }
        }
    }
}
