using UnityEngine;
using DG.Tweening;

public class GameModePanelAnimator : MonoBehaviour
{
    [Header("Slide-In Targets")]
    public RectTransform[] slotTransforms;

    [Header("Slide Settings")]
    public float slideDuration = 0.5f;
    public float delayBetween = 0.15f;
    public float offsetX = 1000f;

    private Vector2[] originalPositions;

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
        for (int i = 0; i < slotTransforms.Length; i++)
        {
            slotTransforms[i].DOAnchorPos(originalPositions[i], slideDuration)
                .SetEase(Ease.OutCubic)
                .SetDelay(i * delayBetween);
        }
    }

    public void ResetSlotsOffscreen()
    {
        for (int i = 0; i < slotTransforms.Length; i++)
        {
            slotTransforms[i].anchoredPosition = originalPositions[i] + new Vector2(offsetX, 0);
        }
    }
}