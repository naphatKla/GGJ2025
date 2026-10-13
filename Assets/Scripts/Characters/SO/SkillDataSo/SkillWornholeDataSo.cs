using System.Collections.Generic;
using GameControl.Pattern;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    /// <summary>
    /// Data for Bright2's "Wornhole" (Design Ver 0.0.20) - phases 2 and 3.
    /// <para/>
    /// "Choose between EnemyPattern or MapEvent then spawn 3 to 8 patterns of those on the map." One cast
    /// commits to a single kind and then drops that many of it, one per interval - it does not mix the two
    /// within a cast.
    /// <para/>
    /// The boss does not own any of what it summons: enemy patterns come from the map's own pattern list
    /// and map events from whatever is registered on the map, so Wornhole always throws the current map's
    /// own content back at the player.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillWornholeData", menuName = "GameData/SkillData/TheBright2Only/Wornhole")]
    public class SkillWornholeDataSo : BaseSkillDataSo
    {
        [FoldoutGroup("Wornhole")]
        [LabelText("Spawn Count Min")]
        [MinValue(0)]
        [PropertyTooltip("Lowest number of patterns a single cast drops. Spec: 3")]
        [SerializeField] private int minSpawnCount = 3;

        [FoldoutGroup("Wornhole")]
        [LabelText("Spawn Count Max")]
        [MinValue(0)]
        [PropertyTooltip("Highest number of patterns a single cast drops, rolled per cast. Spec: 8")]
        [SerializeField] private int maxSpawnCount = 8;

        [FoldoutGroup("Wornhole")]
        [LabelText("Delay Before First Spawn")]
        [Unit(Units.Second), MinValue(0f)]
        [PropertyTooltip("Wind-up before the first pattern appears, so the cast reads as a tell rather than "
                         + "things popping in the instant the skill fires. Not in the doc - 0 keeps the "
                         + "original behaviour.")]
        [SerializeField] private float delayBeforeFirstSpawn;

        [FoldoutGroup("Wornhole")]
        [LabelText("Interval Between Spawns")]
        [Unit(Units.Second), MinValue(0f)]
        [PropertyTooltip("Gap between each spawn. Applies to BOTH enemy patterns and map events. Spec: 1s")]
        [SerializeField] private float intervalBetweenSpawns = 1f;

        [FoldoutGroup("Wornhole")]
        [LabelText("Map Event Chance")]
        [Unit(Units.Percent)]
        [PropertyRange(0, 100)]
        [PropertyTooltip("Chance the cast picks MapEvents. Otherwise it picks EnemyPatterns. 0 = always "
                         + "enemy patterns, 100 = always map events.")]
        [SerializeField] private float mapEventChance = 50f;

        [FoldoutGroup("Wornhole")]
        [LabelText("Enemy Pattern Filter")]
        [PropertyTooltip("Leave EMPTY for the doc's behaviour - any spawn pattern the current map has "
                         + "enabled. Drag patterns in to restrict Wornhole to just those.")]
        [SerializeField] private List<BaseSpawnPattern> enemyPatternFilter = new();

        [FoldoutGroup("Wornhole")]
        [LabelText("Map Event Id Filter")]
        [PropertyTooltip("Leave EMPTY for the doc's behaviour - all map events available on the current map. "
                         + "Fill it in to restrict Wornhole to specific ids (e.g. keep it from summoning "
                         + "Black Holes, which Devourer can then eat for a Special Interaction on the boss "
                         + "itself).")]
        [SerializeField] private List<string> mapEventIdFilter = new();

        public int MinSpawnCount => minSpawnCount;
        public int MaxSpawnCount => Mathf.Max(minSpawnCount, maxSpawnCount);
        public float DelayBeforeFirstSpawn => delayBeforeFirstSpawn;
        public float IntervalBetweenSpawns => intervalBetweenSpawns;
        public float MapEventChance => mapEventChance;
        public List<BaseSpawnPattern> EnemyPatternFilter => enemyPatternFilter;
        public List<string> MapEventIdFilter => mapEventIdFilter;
    }
}
