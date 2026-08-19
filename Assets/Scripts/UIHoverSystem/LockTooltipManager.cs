using TMPro;
using UnityEngine;

public class LockTooltipManager : MonoBehaviour
{
    public static LockTooltipManager Instance { get; private set; }

    [SerializeField] private RectTransform tooltipPanel;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private float showDelay  = 0.6f;
    [SerializeField] private float fadeOutTime = 0.3f;
    [SerializeField] private Vector2 mouseOffset = new Vector2(0f, 16f); 

    private Canvas _rootCanvas;
    private float  _hoverTimer;
    private bool   _isHovering;
    private bool   _isVisible;
    private float  _fadeTimer;

    private void Awake()
    {
        Instance = this;
        if (tooltipPanel != null)
        {
            // หา Canvas ที่ใกล้ที่สุด (ไม่ต้องเป็น root)
            var canvas = tooltipPanel.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                _rootCanvas = canvas;
                canvasRect  = canvas.GetComponent<RectTransform>();
            }
        }
        tooltipPanel.gameObject.SetActive(false);
    }

    public void ShowAtMouse(Vector2 screenPos, string hint)
    {
        hintText.text = string.IsNullOrEmpty(hint) ? "Locked" : hint;

        _isHovering = true;
        _isVisible  = true;
        _hoverTimer = 0f;
        _fadeTimer  = 0f;

        if (canvasGroup != null)
        {
            canvasGroup.alpha         = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable   = false;
        }
        tooltipPanel.gameObject.SetActive(true);

        PositionTooltip(screenPos);
    }

    private void PositionTooltip(Vector2 screenPos)
    {
        if (tooltipPanel == null) return;

        var tooltipParent = tooltipPanel.parent as RectTransform;
        if (tooltipParent == null) return;

        Camera cam = _rootCanvas != null && _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? _rootCanvas.worldCamera
            : null;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            tooltipParent, screenPos, cam, out Vector2 localPos);

        Vector2 tooltipSize = tooltipPanel.rect.size;
        Vector2 parentHalf  = tooltipParent.rect.size * 0.5f;

        // ── Horizontal: default right side, flip left if overflow ──
        //  ใช้ขอบ tooltip (ไม่ใช่ center) เป็นจุดอ้างอิง
        float leftEdge  = localPos.x + mouseOffset.x;
        float posX      = leftEdge + tooltipSize.x * 0.5f;   // center = leftEdge + halfW

        if (posX + tooltipSize.x * 0.5f > parentHalf.x)      // ขวาเกินขอบ
        {
            float rightEdge = localPos.x - mouseOffset.x;
            posX = rightEdge - tooltipSize.x * 0.5f;          // center = rightEdge - halfW
        }

        // ── Vertical: clamp inside parent ──
        float posY = localPos.y + mouseOffset.y;
        if (posY + tooltipSize.y * 0.5f > parentHalf.y)
            posY = parentHalf.y - tooltipSize.y * 0.5f;
        else if (posY - tooltipSize.y * 0.5f < -parentHalf.y)
            posY = -parentHalf.y + tooltipSize.y * 0.5f;

        tooltipPanel.pivot            = new Vector2(0.5f, 0.5f);
        tooltipPanel.anchoredPosition = new Vector2(posX, posY);

        Debug.Log($"[Tooltip] local={localPos} offset={mouseOffset} " +
                  $"final=({posX:0},{posY:0}) parentHalf={parentHalf} size={tooltipSize}");
    }

    public void Hide()
    {
        _isHovering = false;
        _fadeTimer  = 0f;
    }

    public void HideImmediate()
    {
        _isHovering = false;
        _isVisible  = false;
        _hoverTimer = 0f;
        _fadeTimer  = 0f;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        tooltipPanel.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!tooltipPanel.gameObject.activeSelf) return;

        // Follow mouse while visible
        if (_isHovering)
            PositionTooltip(Input.mousePosition);

        if (!_isHovering && _isVisible)
        {
            _fadeTimer += Time.deltaTime;
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(1f, 0f, _fadeTimer / fadeOutTime);
            if (_fadeTimer >= fadeOutTime)
            {
                tooltipPanel.gameObject.SetActive(false);
                _isVisible = false;
            }
        }
    }
}
