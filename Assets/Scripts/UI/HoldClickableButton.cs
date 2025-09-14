using System;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class HoldClickableButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public float minHoldDuration = 2f;
    public event Action OnHoldSuccess;

    Tween _timer;

    public void OnPointerDown(PointerEventData e)
    {
        CancelTimer();
        _timer = DOVirtual.DelayedCall(minHoldDuration, () => SuccessHolding())
            .SetUpdate(UpdateType.Normal, true);
    }

    public void SuccessHolding()
    {
        OnHoldSuccess?.Invoke();
    }

    public void OnPointerUp(PointerEventData e)   => CancelTimer();
    public void OnPointerExit(PointerEventData e) => CancelTimer();
    void OnDisable()                              => CancelTimer();

    void CancelTimer()
    {
        if (_timer != null && _timer.IsActive()) _timer.Kill();
        _timer = null;
    }
}