using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace PermanentUpgrade
{
    public enum PermanentUpgradeType
    {
        MaxHealth,
        Speed,
        Damage,
        CritRate,
        CritDamage,
        LifeStealChance,
        LifeStealEffective,
        ExpMultiply,
        PickupRadius,
        HurtIFrame,
    }
    
    public enum PermanentUpgradeValueMode
    {
        Flat,      // บวกตรง ๆ เช่น +20 HP
        Percent    // +% เช่น +5% Attack
    }

    [Serializable]
    public class PermanentUpgradeLevelData
    {
        [Min(0)] public int nanoCost;
        public float value;
    }

    [Serializable]
    public class PermanentUpgradeEntry
    {
        [FoldoutGroup("$type")]
        public PermanentUpgradeType type;
        
        //Display Data
        [FoldoutGroup("$type")]
        public string nameUpgrade;
        [FoldoutGroup("$type")]
        public Sprite iconUpgrade;
        [FoldoutGroup("$type")] [TextArea(4, 10)]
        public string descriptionUpgrade;
        [FoldoutGroup("$type")] [TextArea(4, 10)]
        public string lockDescriptionUpgrade;
        
        [Header("Value Config")] [FoldoutGroup("$type")]
        public PermanentUpgradeValueMode mode = PermanentUpgradeValueMode.Flat;

        [Tooltip("เลเวลเรียงจาก 1,2,3,...; ขนาด list = max level")] [FoldoutGroup("$type")]
        public List<PermanentUpgradeLevelData> levels = new List<PermanentUpgradeLevelData>();
        [FoldoutGroup("$type")]
        public int MaxLevel => levels != null ? levels.Count : 0;

        public PermanentUpgradeLevelData GetLevelData(int level)
        {
            if (levels == null || level <= 0 || level > levels.Count) return null;
            return levels[level - 1];
        }

        /// <summary>
        /// คืนค่า total bonus ของเลเวลที่กำหนด (สะสมตั้งแต่เลเวล 1→level)
        /// </summary>
        public float GetTotalBonus(int level)
        {
            if (levels == null || level <= 0) return 0f;
            level = Mathf.Clamp(level, 0, levels.Count);

            float sum = 0f;
            for (int i = 0; i < level; i++)
                sum += levels[i].value;

            return sum;
        }
    }
}