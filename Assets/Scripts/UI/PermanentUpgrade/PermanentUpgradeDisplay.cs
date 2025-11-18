using PermanentUpgrade;
using Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.PermanentUpgrade
{
    public class PermanentUpgradeDisplay : MonoBehaviour
    {
        public TMP_Text currentNanoCoinText;
        public TMP_Text nameText;
        public TMP_Text costText;
        public TMP_Text descriptionText;
        public Image imageDisplay;
        public Button upgradeButton;

        public void UpdatePermanentDisplay(PermanentUpgradeConfig config, PermanentUpgradeEntry pEntry, PlayerData playerData)
        {
            if (currentNanoCoinText != null) currentNanoCoinText.text = playerData.nanoCoin.ToString();
            if (nameText != null) nameText.text = pEntry.nameUpgrade;
            if (costText != null) costText.text = GetCurrentLevel(config,pEntry,playerData);
            if (descriptionText != null) descriptionText.text = pEntry.descriptionUpgrade;
            if (imageDisplay != null) imageDisplay.sprite = pEntry.iconUpgrade;
        }
        
        private string GetCurrentLevel(PermanentUpgradeConfig config, PermanentUpgradeEntry pEntry, PlayerData playerData)
        {
            var entry = config.GetEntry(pEntry.type);
            if (entry == null) return "NULL";

            var currentLevel = playerData.GetPermanentUpgradeLevel(pEntry.type);
            var nextLevel = currentLevel + 1;
            if (currentLevel == entry.MaxLevel) return "MAX UPGRADE";

            var lvlData = entry.GetLevelData(nextLevel);
            return "COST " +lvlData.nanoCost.ToString();
        }
    }
}