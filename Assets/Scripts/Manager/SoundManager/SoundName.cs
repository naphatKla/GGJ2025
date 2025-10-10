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
            private const string Group = nameof(SFX);
            public const string PlayerAttackHit = Group + "/" + nameof(PlayerAttackHit);
            public const string PlayerCounterDash = Group + "/" + nameof(PlayerCounterDash);
            public const string PlayerHurt = Group + "/" + nameof(PlayerHurt);
            public const string PlayerHeal = Group + "/" + nameof(PlayerHeal);
            public const string PlayerDead = Group + "/" + nameof(PlayerDead);
            public const string PlayerCollectAnergy = Group + "/" + nameof(PlayerCollectAnergy);
            public const string PlayerLevelUp = Group + "/" + nameof(PlayerLevelUp);
            public const string PlayerDash = Group + "/" + nameof(PlayerDash);
            public const string PlayerParryUse = Group + "/" + nameof(PlayerParryUse);
            public const string PlayerParrySuccess = Group + "/" + nameof(PlayerParrySuccess);
            public const string PlayerReflection = Group + "/" + nameof(PlayerReflection);
            public const string PlayerLightStepUse = Group + "/" + nameof(PlayerLightStepUse);
            public const string PlayerLightStepEnd = Group + "/" + nameof(PlayerLightStepEnd);
            public const string PlayerHarmonyOfLight = Group + "/" + nameof(PlayerHarmonyOfLight);
            public const string PlayerOverloop = Group + "/" + nameof(PlayerOverloop);
            public const string PlayerBaseDown = Group + "/" + nameof(PlayerBaseDown);
            public const string MapEventLaserNotify = Group + "/" + nameof(MapEventLaserNotify);
            public const string MapEventLaser = Group + "/" + nameof(MapEventLaser);
            public const string PiercerSkillDash = Group + "/" + nameof(PiercerSkillDash);
            public const string PressureSkillExplosion = Group + "/" + nameof(PressureSkillExplosion);
            public const string FireCrackerSkillExplosion = Group + "/" + nameof(FireCrackerSkillExplosion);
            public const string BomberSkillExplosion = Group + "/" + nameof(BomberSkillExplosion);
            public const string AbsorptionSkillParryUse = Group + "/" + nameof(AbsorptionSkillParryUse);
            public const string AbsorptionSkillParrySuccess = Group + "/" + nameof(AbsorptionSkillParrySuccess);
        }

        public static class UI
        {
            private const string Group = nameof(UI);

            public const string Click        = Group + "/" + nameof(Click);
            public const string Hover        = Group + "/" + nameof(Hover);
            public const string Confirm      = Group + "/" + nameof(Confirm);
            public const string Cancel       = Group + "/" + nameof(Cancel);
            public const string OpenWindow   = Group + "/" + nameof(OpenWindow);
            public const string CloseWindow  = Group + "/" + nameof(CloseWindow);
            public const string ToggleOn     = Group + "/" + nameof(ToggleOn);
            public const string ToggleOff    = Group + "/" + nameof(ToggleOff);
            public const string Notification = Group + "/" + nameof(Notification);
            public const string Reward       = Group + "/" + nameof(Reward);
            public const string Error        = Group + "/" + nameof(Error);
            public const string CardSelection = Group + "/" + nameof(CardSelection);
            public const string Gameplay_CountDown5Sec        = Group + "/" + nameof(Gameplay_CountDown5Sec);
            public const string Gameplay_WarningAlert = Group + "/" + nameof(Gameplay_WarningAlert);
            public const string Gameplay_SkillNotify = Group + "/" + nameof(Gameplay_SkillNotify);
            public const string Gameplay_ActionTextNotification = Group + "/" + nameof(Gameplay_ActionTextNotification);
            public const string Gameplay_MainSkillCooldownReady = Group + "/" + nameof(Gameplay_MainSkillCooldownReady);
            public const string Gameplay_AutoSkillCooldownReady = Group + "/" + nameof(Gameplay_AutoSkillCooldownReady);
        }

        public static class BGM
        {
            private const string Group = nameof(BGM);
            public const string GamePlayPhase1 = Group + "/" + nameof(GamePlayPhase1);
            public const string GamePlayPhase2 = Group + "/" + nameof(GamePlayPhase2);
            public const string Boss = Group + "/" + nameof(Boss);
            public const string MainMenu = Group + "/" + nameof(MainMenu);
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
