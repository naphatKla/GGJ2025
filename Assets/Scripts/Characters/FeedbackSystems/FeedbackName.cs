using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;

namespace Characters.FeedbackSystems
{
    /// <summary>
    /// String keys แยกกลุ่ม + โครงสร้างสำหรับ Odin Dropdown/Sorting
    /// </summary>
    public static class FeedbackName
    {
        // --------- GROUPS ---------
        public static class Character
        {
            public const string AttackHit = "Character/AttackHit";
            public const string CounterAttack = "Character/CounterAttack";
            public const string TakeDamage = "Character/TakeDamage";
            public const string Heal = "Character/Heal";
            public const string Iframe = "Character/Iframe";
            public const string Dead = "Character/Dead";
            public const string Spawn = "Character/Spawn";
            public const string NotifySkill = "Character/NotifySkill";
        }

        public static class Skill
        {
            public const string DashLv1 = "Skill/Dash_Lv1";
            public const string DashLv2 = "Skill/Dash_Lv2";
            public const string DashLv3 = "Skill/Dash_Lv3";
            public const string DashLv4 = "Skill/Dash_Lv4";
            public const string DashLv5 = "Skill/Dash_Lv5";
            public const string DashLv6 = "Skill/Dash_Lv6";
            public const string ParryUseLv1 = "Skill/ParryUse_Lv1";
            public const string ParryUseLv2 = "Skill/ParryUse_Lv2";
            public const string ParryUseLv3 = "Skill/ParryUse_Lv3";
            public const string ParryUseLv4 = "Skill/ParryUse_Lv4";
            public const string ParryUseLv5 = "Skill/ParryUse_Lv5";
            public const string ParryUseLv6 = "Skill/ParryUse_Lv6";
            public const string ParrySuccessLv1 = "Skill/ParrySuccess_Lv1";
            public const string ParrySuccessLv2 = "Skill/ParrySuccess_Lv2";
            public const string ParrySuccessLv3 = "Skill/ParrySuccess_Lv3";
            public const string ParrySuccessLv4 = "Skill/ParrySuccess_Lv4";
            public const string ParrySuccessLv5 = "Skill/ParrySuccess_Lv5";
            public const string ParrySuccessLv6 = "Skill/ParrySuccess_Lv6";
            public const string ReflectionLv1 = "Skill/Reflection_Lv1";
            public const string ReflectionLv2 = "Skill/Reflection_Lv2";
            public const string ReflectionLv3 = "Skill/Reflection_Lv3";
            public const string ReflectionLv4 = "Skill/Reflection_Lv4";
            public const string ReflectionLv5 = "Skill/Reflection_Lv5";
            public const string ReflectionLv6 = "Skill/Reflection_Lv6";
            public const string LightStepUseLv1 = "Skill/LightStepUse_Lv1";
            public const string LightStepUseLv2 = "Skill/LightStepUse_Lv2";
            public const string LightStepUseLv3 = "Skill/LightStepUse_Lv3";
            public const string LightStepUseLv4 = "Skill/LightStepUse_Lv4";
            public const string LightStepUseLv5 = "Skill/LightStepUse_Lv5";
            public const string LightStepUseLv6 = "Skill/LightStepUse_Lv6";
            public const string LightStepEndLv1 = "Skill/LightStepEnd_Lv1";
            public const string LightStepEndLv2 = "Skill/LightStepEnd_Lv2";
            public const string LightStepEndLv3 = "Skill/LightStepEnd_Lv3";
            public const string LightStepEndLv4 = "Skill/LightStepEnd_Lv4";
            public const string LightStepEndLv5 = "Skill/LightStepEnd_Lv5";
            public const string LightStepEndLv6 = "Skill/LightStepEnd_Lv6";
            public const string HarmonyOfLightLv1 = "Skill/HarmonyOfLight_Lv1";
            public const string HarmonyOfLightLv2 = "Skill/HarmonyOfLight_Lv2";
            public const string HarmonyOfLightLv3 = "Skill/HarmonyOfLight_Lv3";
            public const string HarmonyOfLightLv4 = "Skill/HarmonyOfLight_Lv4";
            public const string HarmonyOfLightLv5 = "Skill/HarmonyOfLight_Lv5";
            public const string HarmonyOfLightLv6 = "Skill/HarmonyOfLight_Lv6";
            public const string PiercerDashChargeLv1 = "Skill/PiercerDashCharge_Lv1";
            public const string ChargeBombLv1 = "Skill/ChargeBomb_Lv1";
            public const string PressureBombLv1 = "Skill/PressureBomb_Lv1";
        }

        // ---------- Caches ----------
        private static IReadOnlyList<string> _allKeys;
        private static HashSet<string> _allSet;
        private static Dictionary<string, List<string>> _orderedByGroup; // "Skill" -> ordered list

        /// <summary>ลิสต์คีย์ทั้งหมด (เรียงตามลำดับประกาศจริง)</summary>
        public static IReadOnlyList<string> AllKeys => _allKeys ??= BuildAllKeysOrdered();

        /// <summary>คีย์เฉพาะกลุ่ม เรียงตามลำดับประกาศ</summary>
        public static IReadOnlyList<string> OrderedKeysOf(string groupName)
        {
            EnsureCaches();
            return _orderedByGroup != null && _orderedByGroup.TryGetValue(groupName, out var list)
                ? list
                : Array.Empty<string>();
        }

        public static bool IsValid(string key)
        {
            _allSet ??= new HashSet<string>(AllKeys);
            return _allSet.Contains(key);
        }

        public static IEnumerable<string> Groups()
        {
            return typeof(FeedbackName)
                .GetNestedTypes(BindingFlags.Public)
                .Where(t => t.IsClass)
                .OrderBy(t => t.MetadataToken) // ตามลำดับประกาศคลาส
                .Select(t => t.Name);
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
            return shortName; // fallback
        }

#if UNITY_EDITOR
        public static class Odin
        {
            public static IEnumerable<ValueDropdownItem<string>> ShortGroup(string groupName)
            {
                foreach (var e in EnumerateEntriesOrdered())
                {
                    if (e.Group != groupName) continue;
                    var label = ShortLabel(e.Value);
                    yield return new ValueDropdownItem<string>(label, label); // value = short name
                }
            }

            // ⬇️ เวอร์ชันที่มีค่าว่าง
            public static IEnumerable<ValueDropdownItem<string>> ShortGroupWithNone(string groupName)
            {
                // ใช้ "" เป็นค่าว่าง (หรือจะใช้ null ก็ได้ แต่แนะนำ "")
                yield return new ValueDropdownItem<string>("<None>", "");

                foreach (var e in EnumerateEntriesOrdered())
                {
                    if (e.Group != groupName) continue;
                    var label = ShortLabel(e.Value);
                    yield return new ValueDropdownItem<string>(label, label);
                }
            }
        }
#endif


        // ---------- internals ----------
        private struct Entry
        {
            public string Group;
            public string Name;
            public string Value;
            public string DisplayPath => $"{Group}/{Name}";
        }

        private static void EnsureCaches()
        {
            if (_orderedByGroup == null || _allKeys == null)
                _allKeys = BuildAllKeysOrdered();
        }

        private static IReadOnlyList<string> BuildAllKeysOrdered()
        {
            _orderedByGroup = new Dictionary<string, List<string>>();
            var list = new List<string>(64);

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
            var groups = typeof(FeedbackName)
                .GetNestedTypes(BindingFlags.Public)
                .Where(t => t.IsClass)
                .OrderBy(t => t.MetadataToken); // กลุ่มตามประกาศจริง

            foreach (var g in groups)
            {
                string groupName = g.Name;

                var fields = g.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                    .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                    .OrderBy(f => f.MetadataToken); // ฟิลด์ตามประกาศจริง

                foreach (var f in fields)
                {
                    string value = (string)f.GetRawConstantValue();
                    yield return new Entry
                    {
                        Group = groupName,
                        Name = f.Name,
                        Value = value
                    };
                }
            }
        }
    }
}