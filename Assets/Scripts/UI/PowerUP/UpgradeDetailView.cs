using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// View ของ panel ด้านขวา แสดงข้อมูลของ Upgrade ที่เลือกอยู่
/// </summary>
public class UpgradeDetailView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI costText;

    public void Show(UpgradeItemConfig cfg, int currentLevel, int cost)
    {
        if (cfg == null) return;

        if (iconImage != null)
        {
            iconImage.sprite = cfg.icon;
            iconImage.enabled = true;
        }

        if (nameText != null)
            nameText.text = cfg.displayName;

        if (descriptionText != null)
            descriptionText.text = cfg.description;

        if (costText != null)
            costText.text = $"COST {cost}";
    }

    public void Clear()
    {
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (nameText != null)        nameText.text = "";
        if (descriptionText != null) descriptionText.text = "";
        if (costText != null)        costText.text = "COST -";
    }
}