using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;

public class UIButtonEffectManager : MonoBehaviour
{
    [Header("Assign Buttons")]
    [SerializeField] private List<Button> buttonsWithEffect;

    [Header("Hover & Click Settings")]
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float pressScale = 1.15f;
    [SerializeField] private float tweenDuration = 0.2f;
    [SerializeField] private Ease easeType = Ease.OutBack;

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

        // Add Event Handlers
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();

        AddTrigger(trigger, EventTriggerType.PointerEnter, (data) =>
        {
            rectTransform.DOScale(originalScale * hoverScale, tweenDuration).SetEase(easeType);
        });

        AddTrigger(trigger, EventTriggerType.PointerExit, (data) =>
        {
            rectTransform.DOScale(originalScale, tweenDuration).SetEase(Ease.OutQuad);
        });

        AddTrigger(trigger, EventTriggerType.PointerDown, (data) =>
        {
            rectTransform.DOScale(originalScale * pressScale, tweenDuration).SetEase(easeType);
        });

        AddTrigger(trigger, EventTriggerType.PointerUp, (data) =>
        {
            bool isHover = ((PointerEventData)data).pointerEnter == button.gameObject;
            float targetScale = isHover ? hoverScale : 1f;
            rectTransform.DOScale(originalScale * targetScale, tweenDuration).SetEase(Ease.OutBack);
        });
    }

    void AddTrigger(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }
}
