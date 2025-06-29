using UnityEngine;
using DG.Tweening;
using UnityEngine.UI; // สำหรับปิด interaction

public class GameModePanelSlide : MonoBehaviour
{
    [Header("Slide-In Targets")]
    public RectTransform[] slotTransforms;

    [Header("Slide Settings")]
    public float slideDuration = 0.5f;
    public float delayBetween = 0.15f;
    public float offsetX = 1000f;

    private Vector2[] originalPositions;
    public static bool isSliding = false;

    private void Awake()
    {
        originalPositions = new Vector2[slotTransforms.Length];

        for (int i = 0; i < slotTransforms.Length; i++)
        {
            originalPositions[i] = slotTransforms[i].anchoredPosition;
        }
    }

    private void OnEnable()
    {
        ResetSlotsOffscreen();
        SlideIn();
    }

    public void SlideIn()
    {
        isSliding = true;
        int total = slotTransforms.Length;
        int completed = 0;

        for (int i = 0; i < total; i++)
        {
            int index = i;
            RectTransform slot = slotTransforms[index];

            slot.DOAnchorPos(originalPositions[index], slideDuration)
                .SetEase(Ease.OutCubic)
                .SetDelay(index * delayBetween)
                .OnComplete(() =>
                {
                    completed++;
                    if (completed >= total)
                        isSliding = false; // <-- ปลดล็อกหลังจากทั้งหมดเสร็จ
                });
        }
    }

    public void ResetSlotsOffscreen()
    {
        for (int i = 0; i < slotTransforms.Length; i++)
        {
            slotTransforms[i].anchoredPosition = originalPositions[i] + new Vector2(offsetX, 0);
            slotTransforms[i].localScale = Vector3.one;
        }
    }
}