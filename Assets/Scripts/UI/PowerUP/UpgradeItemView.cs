using System;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeItemView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject lockRoot;
    [SerializeField] private Image selectedFrame;
    [SerializeField] private UpgradeLevelBarView levelBar;
    [SerializeField] private Button clickAreaButton;

    /// <summary>ยิงตอนการ์ดนี้ถูกคลิก</summary>
    public event Action OnClick;

    private void Awake()
    {
        if (clickAreaButton != null)
        {
            clickAreaButton.onClick.AddListener(() =>
            {
                Debug.Log($"[UpgradeItemView] Clicked card: {name}");
                OnClick?.Invoke();
            });
        }
        else
        {
            Debug.LogWarning($"[UpgradeItemView] {name} has no clickAreaButton assigned.");
        }

        // ให้กรอบเลือกปิดไว้ก่อนตอนเริ่ม
        if (selectedFrame != null)
            selectedFrame.gameObject.SetActive(false);
    }

    public void SetIcon(Sprite sprite)
    {
        if (iconImage == null) return;

        if (sprite != null)
        {
            iconImage.sprite = sprite;
            iconImage.enabled = true;
        }
        else
        {
            // ไม่มีไอคอน แต่ยังอยากให้สี่เหลี่ยมการ์ดโชว์อยู่
            iconImage.enabled = true;
        }
    }

    public void SetLocked(bool locked)
    {
        if (lockRoot != null)
            lockRoot.SetActive(locked);

        if (clickAreaButton != null)
            clickAreaButton.interactable = !locked;
    }

    public void SetSelected(bool selected)
    {
        if (selectedFrame == null)
        {
            Debug.LogWarning($"[UpgradeItemView] {name} has no selectedFrame assigned.");
            return;
        }

        selectedFrame.gameObject.SetActive(selected);
        Debug.Log($"[UpgradeItemView] {name} SetSelected({selected})");
    }

    /// <summary>เรียกตอน init การ์ด กำหนดจำนวนช่อง + เลเวลเริ่มต้น</summary>
    public void InitLevelBar(int maxLevel, int currentLevel)
    {
        if (levelBar == null)
        {
            Debug.LogWarning($"[UpgradeItemView] {name} has no levelBar assigned.");
            return;
        }

        levelBar.Init(maxLevel, currentLevel);
    }

    /// <summary>เรียกตอนอัพเกรด เลเวลเปลี่ยน</summary>
    public void SetLevel(int level)
    {
        if (levelBar == null) return;
        levelBar.SetLevel(level);
    }
}
