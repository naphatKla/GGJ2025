using UnityEngine;
using UnityEngine.EventSystems;

public class LockTooltipTrigger : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    private bool _isLocked;
    private string _hint;

    public void SetLock(bool isLocked, string hint)
    {
        _isLocked = isLocked;
        _hint = hint;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_isLocked) return;
        LockTooltipManager.Instance.HideImmediate();
        LockTooltipManager.Instance.ShowAtRect(GetComponent<RectTransform>(), _hint);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        LockTooltipManager.Instance.Hide();
    }
}