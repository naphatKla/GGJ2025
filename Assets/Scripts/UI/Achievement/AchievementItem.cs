using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button claimButton;
    [SerializeField] private TMP_Text claimText;
    [SerializeField] private TMP_Text completeText;
    [SerializeField] private GameObject claimBlock;

    [Header("Button Colors")]
    [SerializeField] private Color buttonNormalColor = new Color32(0, 200, 255, 255);
    [SerializeField] private Color buttonCompleteColor = new Color32(90, 90, 90, 255);

    private bool isClaimed;

    private void Start()
    {
        completeText.gameObject.SetActive(false);
        claimBlock.SetActive(false);
        
        var cb = claimButton.colors;
        cb.normalColor = buttonNormalColor;
        claimButton.colors = cb;

        claimButton.onClick.AddListener(OnClaim);
    }

    private void OnClaim()
    {
        if (isClaimed) return;
        isClaimed = true;
        
        claimText.gameObject.SetActive(false);
        completeText.gameObject.SetActive(true);
        
        var cb = claimButton.colors;
        cb.normalColor = buttonCompleteColor;
        cb.highlightedColor = buttonCompleteColor;
        cb.pressedColor = buttonCompleteColor;
        cb.selectedColor = buttonCompleteColor;
        cb.disabledColor = buttonCompleteColor;
        claimButton.colors = cb;
        
        claimButton.interactable = false;
        
        claimBlock.SetActive(true);
    }
}