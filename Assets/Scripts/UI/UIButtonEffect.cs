using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;

public class UIButtonEffect : MonoBehaviour
{
    [Header("Assign Buttons")]
    [SerializeField] private List<Button> buttonsWithEffect;

    [Header("Hover & Click Settings")]
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float pressScale = 1.15f;
    [SerializeField] private float tweenDuration = 0.2f;
    [SerializeField] private Ease easeType = Ease.OutBack;

    [Header("Alpha Settings")]
    [SerializeField] private float normalAlpha = 0f;
    [SerializeField] private float hoverAlpha = 1f;

    void Start()
    {
        foreach (Button button in buttonsWithEffect)
        {
            AddEffectToButton(button);
        }
    }

    void AddEffectToButton(Button button)
    {
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        Vector3 originalScale = rectTransform.localScale;

        Image backgroundImage = button.GetComponent<Image>();
        if (backgroundImage != null)
        {
            Color c = backgroundImage.color;
            c.a = normalAlpha;
            backgroundImage.color = c;
        }

        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();

        AddTrigger(trigger, EventTriggerType.PointerEnter, (data) =>
        {
            rectTransform.DOKill();
            rectTransform.DOScale(originalScale * hoverScale, tweenDuration)
                .SetEase(easeType)
                .SetUpdate(true);

            if (backgroundImage != null)
            {
                backgroundImage.DOFade(hoverAlpha, tweenDuration)
                    .SetUpdate(true);
            }
        });

        AddTrigger(trigger, EventTriggerType.PointerExit, (data) =>
        {
            rectTransform.DOKill();
            rectTransform.DOScale(originalScale, tweenDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);

            if (backgroundImage != null)
            {
                backgroundImage.DOFade(normalAlpha, tweenDuration)
                    .SetUpdate(true);
            }
        });

        AddTrigger(trigger, EventTriggerType.PointerDown, (data) =>
        {
            rectTransform.DOKill();
            rectTransform.DOScale(originalScale * pressScale, tweenDuration)
                .SetEase(easeType)
                .SetUpdate(true);
        });

        AddTrigger(trigger, EventTriggerType.PointerUp, (data) =>
        {
            rectTransform.DOKill();
            bool isHover = ((PointerEventData)data).pointerEnter == button.gameObject;
            if (isHover)
            {
                rectTransform.DOScale(originalScale * hoverScale, tweenDuration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);
            }
            else
            {
                ResetEffect(rectTransform, backgroundImage, originalScale);
            }
        });

        // Reset effect after button click
        button.onClick.AddListener(() =>
        {
            ResetEffect(rectTransform, backgroundImage, originalScale);
        });
    }

    void ResetEffect(RectTransform rectTransform, Image backgroundImage, Vector3 originalScale)
    {
        rectTransform.DOKill();
        rectTransform.DOScale(originalScale, tweenDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        if (backgroundImage != null)
        {
            backgroundImage.DOFade(normalAlpha, tweenDuration)
                .SetUpdate(true);
        }
    }

    void AddTrigger(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener((data) =>
        {
            if (GameModePanelSlide.isSliding) return; // กันคลิกขณะ slide
            action.Invoke(data);
        });
        trigger.triggers.Add(entry);
    }
}
