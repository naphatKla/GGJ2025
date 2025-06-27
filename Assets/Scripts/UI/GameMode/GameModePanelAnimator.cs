using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class GameModePanelAnimator : MonoBehaviour
{
    [Header("Slide-In Targets")]
    public RectTransform[] slotTransforms;

    [Header("Slide Settings")]
    public float slideDuration = 0.5f;
    public float delayBetween = 0.15f;
    public float offsetX = 1000f;

    [Header("Hover Settings")]
    public float hoverScale = 1.05f;
    public float tweenDuration = 0.2f;
    public Ease easeType = Ease.OutBack;

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
            int index = i;
            RectTransform slot = slotTransforms[index];

            slot.DOAnchorPos(originalPositions[index], slideDuration)
                .SetEase(Ease.OutCubic)
                .SetDelay(index * delayBetween)
                .OnComplete(() =>
                {
                    AddHoverEffect(slot);
                });
        }
    }

    public void ResetSlotsOffscreen()
    {
        for (int i = 0; i < slotTransforms.Length; i++)
        {
            slotTransforms[i].anchoredPosition = originalPositions[i] + new Vector2(offsetX, 0);
            slotTransforms[i].localScale = Vector3.one; // รีเซ็ต scale
        }
    }

    private void AddHoverEffect(RectTransform target)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = target.gameObject.AddComponent<EventTrigger>();

        trigger.triggers.Clear();

        Vector3 originalScale = target.localScale;

        // PointerEnter
        EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener((eventData) =>
        {
            target.DOKill();
            target.DOScale(originalScale * hoverScale, tweenDuration).SetEase(easeType);
        });
        trigger.triggers.Add(enterEntry);

        // PointerExit
        EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener((eventData) =>
        {
            target.DOKill();
            target.DOScale(originalScale, tweenDuration).SetEase(easeType);
        });
        trigger.triggers.Add(exitEntry);
    }
}