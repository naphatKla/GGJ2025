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

    public void ShowAtRect(RectTransform target, string hint)
    {
        hintText.text = string.IsNullOrEmpty(hint) ? "Locked" : hint;

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        // corners[0]=BL  corners[1]=TL  corners[2]=TR  corners[3]=BR

        var tooltipParent = tooltipPanel.parent as RectTransform;
        Camera cam = _rootCanvas != null && _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? _rootCanvas.worldCamera
            : null;

        // ScreenPointToLocalPointInRectangle handles nested-canvas scale correctly
        Vector2 screenBL = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        Vector2 screenTL = RectTransformUtility.WorldToScreenPoint(cam, corners[1]);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(tooltipParent, screenBL, cam, out Vector2 localBL);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(tooltipParent, screenTL, cam, out Vector2 localTL);

        tooltipPanel.pivot = new Vector2(1f, 0.5f);
        tooltipPanel.anchoredPosition = new Vector2(localTL.x - 8f, (localTL.y + localBL.y) * 0.5f);

        _isHovering = true;
        _isVisible  = true;
        _hoverTimer = 0f;
        _fadeTimer  = 0f;

        if (canvasGroup != null) canvasGroup.alpha = 1f;
        tooltipPanel.gameObject.SetActive(true);
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
