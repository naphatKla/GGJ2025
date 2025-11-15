using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SkillTreeViewport : MonoBehaviour
{
    [Header("References")]
    [SerializeField] RectTransform viewport;
    [SerializeField] RectTransform content;

    [Header("Zoom")]
    [SerializeField] float minScale = 0.4f;
    [SerializeField] float maxScale = 2.2f;
    [SerializeField] float zoomStep = 0.15f;
    [SerializeField] float zoomStepCtrl = 0.3f;

    public enum ZoomAnchor { Cursor, ViewportCenter, Target }
    [SerializeField] ZoomAnchor zoomAnchor = ZoomAnchor.ViewportCenter; // << ยึดกลางจอเป็นค่าเริ่มต้น
    [SerializeField] RectTransform lockTarget; // ใช้เมื่อ ZoomAnchor = Target

    [Header("Pan")]
    [SerializeField] float dragPanSpeed = 1f;
    [SerializeField] bool panWithRightMouse = false;

    [Header("Inertia")]
    [SerializeField] bool inertia = true;
    [SerializeField] float inertiaDamp = 12f;

    Vector2 velocity;
    bool dragging;

    Canvas _canvas;
    Camera _cam; // กล้องของ Canvas (Overlay = null)

    void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        _cam = _canvas ? _canvas.worldCamera : null; // Overlay = null
    }

    void Reset() => viewport = GetComponent<RectTransform>();

    void Update()
    {
        if (content == null || viewport == null) return;

        HandleMouseWheelZoom();
        HandlePan();
        HandleHotkeys();

        if (!dragging && inertia)
        {
            if (velocity.sqrMagnitude > 0.0001f)
            {
                Vector2 delta = velocity * Time.unscaledDeltaTime;
                SetAnchoredPositionClamped(content.anchoredPosition + delta);
                velocity = Vector2.Lerp(velocity, Vector2.zero, Time.unscaledDeltaTime * inertiaDamp);
            }
            else velocity = Vector2.zero;
        }
    }

    void HandleMouseWheelZoom()
    {
        if (Mouse.current == null) return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Approximately(scroll, 0f)) return;

        // ซูมเฉพาะเมื่อชี้อยู่บน UI
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject() == false) return;

        float step = (Keyboard.current != null && Keyboard.current.ctrlKey.isPressed) ? zoomStepCtrl : zoomStep;
        float factor = 1f + (scroll > 0 ? step : -step);

        Vector2 screenPoint = GetZoomAnchorScreenPoint();
        ZoomAtScreenPoint(screenPoint, factor);
    }

    void HandlePan()
    {
        if (Mouse.current == null) return;

        bool mmb = Mouse.current.middleButton.isPressed;
        bool rmb = panWithRightMouse && Mouse.current.rightButton.isPressed;
        bool holdSpace = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;

        if ((mmb || rmb || holdSpace) &&
            (EventSystem.current == null || EventSystem.current.IsPointerOverGameObject()))
        {
            dragging = true;
            Vector2 delta = Mouse.current.delta.ReadValue() * dragPanSpeed;
            SetAnchoredPositionClamped(content.anchoredPosition + delta);
            velocity = delta / Time.unscaledDeltaTime;
        }
        else dragging = false;
    }

    void HandleHotkeys()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.fKey.wasPressedThisFrame)
            FitAndCenter();

        if (Keyboard.current.homeKey.wasPressedThisFrame)
        {
            content.localScale = Vector3.one;
            content.anchoredPosition = Vector2.zero;
        }
    }

    Vector2 GetZoomAnchorScreenPoint()
    {
        switch (zoomAnchor)
        {
            case ZoomAnchor.ViewportCenter:
            {
                // จุดกึ่งกลาง viewport (คงตำแหน่งกลางไว้ตอนซูม)
                Vector3 worldCenter = viewport.TransformPoint(viewport.rect.center);
                return RectTransformUtility.WorldToScreenPoint(_cam, worldCenter);
            }
            case ZoomAnchor.Target:
            {
                if (lockTarget != null)
                    return RectTransformUtility.WorldToScreenPoint(_cam, lockTarget.position);
                // ถ้าไม่ได้เซ็ต target ให้ตกไปใช้ Center
                Vector3 worldCenter = viewport.TransformPoint(viewport.rect.center);
                return RectTransformUtility.WorldToScreenPoint(_cam, worldCenter);
            }
            case ZoomAnchor.Cursor:
            default:
                return Mouse.current.position.ReadValue();
        }
    }

    void ZoomAtScreenPoint(Vector2 screenPoint, float factor)
    {
        float current = content.localScale.x;
        float target = Mathf.Clamp(current * factor, minScale, maxScale);
        factor = target / current;

        // แปลง Screen → Local ของ content (อิงกล้องของ Canvas ให้ถูก)
        RectTransformUtility.ScreenPointToLocalPointInRectangle(content, screenPoint, _cam, out var localBefore);

        // เปลี่ยนสเกล
        content.localScale = new Vector3(target, target, 1f);

        // หา local หลังซูม แล้วชดเชยตำแหน่งให้จุดอ้างอิงอยู่ที่เดิม
        RectTransformUtility.ScreenPointToLocalPointInRectangle(content, screenPoint, _cam, out var localAfter);
        Vector2 delta = (localAfter - localBefore);
        SetAnchoredPositionClamped(content.anchoredPosition - delta);
    }

    void SetAnchoredPositionClamped(Vector2 desired)
    {
        Vector2 size = Vector2.Scale(content.rect.size, content.localScale);
        Vector2 vp = viewport.rect.size;

        Vector2 min = new(
            vp.x * 0.5f - size.x * (1f - content.pivot.x),
            vp.y * 0.5f - size.y * (1f - content.pivot.y));
        Vector2 max = new(
            size.x * content.pivot.x - vp.x * 0.5f,
            size.y * content.pivot.y - vp.y * 0.5f);

        desired.x = (min.x > max.x) ? (min.x + max.x) * 0.5f : Mathf.Clamp(desired.x, min.x, max.x);
        desired.y = (min.y > max.y) ? (min.y + max.y) * 0.5f : Mathf.Clamp(desired.y, min.y, max.y);

        content.anchoredPosition = desired;
    }

    [ContextMenu("Fit & Center")]
    public void FitAndCenter()
    {
        if (content == null || viewport == null) return;
        content.localScale = Vector3.one;

        var size = content.rect.size;
        var vp = viewport.rect.size;
        float sx = vp.x / size.x;
        float sy = vp.y / size.y;
        float s = Mathf.Clamp(Mathf.Min(sx, sy) * 0.9f, minScale, maxScale);
        content.localScale = new Vector3(s, s, 1f);
        content.anchoredPosition = Vector2.zero;
    }
}
