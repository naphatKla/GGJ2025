using Player;
using UnityEngine;

namespace PermanentUpgrade
{
   public static class PermanentUpgradeExtensions
    {
        // Call Api
        public static int GetPermanentUpgradeLevel(this PlayerData p, PermanentUpgradeType type)
        {
            if (p == null || p.PermanentUpgrades == null) return 0;
            return p.PermanentUpgrades.TryGetValue(type, out var lv) ? lv : 0;
        }

        // Upgrade Logic
        public static bool CanUpgrade(
            this PlayerData p,
            PermanentUpgradeType type,
            PermanentUpgradeConfig config)
        {
            if (p == null || config == null) return false;

            var entry = config.GetEntry(type);
            if (entry == null) return false;

            var currentLevel = p.GetPermanentUpgradeLevel(type);
            var nextLevel = currentLevel + 1;

            if (nextLevel > entry.MaxLevel) return false;

            var lvlData = entry.GetLevelData(nextLevel);
            if (lvlData == null) return false;

            return p.nanoCoin >= lvlData.nanoCost;
        }

        /// <summary>
        /// อัปเกรด stat ถ้าทำได้ (อัปเลเวล + หัก nanoCoin)
        /// </summary>
        public static bool TryUpgrade(
            this PlayerData p,
            PermanentUpgradeType type,
            PermanentUpgradeConfig config)
        {
            if (p == null || config == null) return false;

            var entry = config.GetEntry(type);
            if (entry == null) return false;

            var currentLevel = p.GetPermanentUpgradeLevel(type);
            var nextLevel = currentLevel + 1;

            if (nextLevel > entry.MaxLevel) return false;

            var lvlData = entry.GetLevelData(nextLevel);
            if (lvlData == null) return false;

            if (p.nanoCoin < lvlData.nanoCost) return false;

            p.nanoCoin -= lvlData.nanoCost;
            p.PermanentUpgrades[type] = nextLevel;

            return true;
        }

        /// <summary>
        /// คืน "โบนัส" จาก Permanent Upgrade ของ type นั้น ๆ (ยังไม่รวม base)
        /// </summary>
        public static float GetPermanentUpgradeBonus(
            this PlayerData p,
            PermanentUpgradeType type,
            PermanentUpgradeConfig config)
        {
            if (p == null || config == null) return 0f;

            var entry = config.GetEntry(type);
            if (entry == null) return 0f;

            var lv = p.GetPermanentUpgradeLevel(type);
            return entry.GetTotalBonus(lv);
        }

        /// <summary>
        /// เอา baseValue (ค่าพื้นฐานของตัวละคร) มาผ่าน PermanentUpgrade แล้วคืนค่า final
        /// </summary>
        public static float ApplyPermanentUpgrade(
            this PlayerData p,
            PermanentUpgradeType type,
            PermanentUpgradeConfig config,
            float baseValue)
        {
            if (p == null || config == null) return baseValue;

            var entry = config.GetEntry(type);
            if (entry == null) return baseValue;

            var bonus = entry.GetTotalBonus(p.GetPermanentUpgradeLevel(type));

            switch (entry.mode)
            {
                case PermanentUpgradeValueMode.Flat:
                    return bonus;

                case PermanentUpgradeValueMode.Percent:
                    return baseValue * (bonus/100);

                default:
                    return baseValue;
            }
        }
    } 
}
