using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class ScrollClamp : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private float maxOffsetX = 1000f;
    [SerializeField] private float maxOffsetY = 1000f;

    private RectTransform content;
    private RectTransform viewport;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (scrollRect == null)
        {
            scrollRect = GetComponent<ScrollRect>();
        }

        if (scrollRect != null)
        {
            content = scrollRect.content;
            viewport = scrollRect.viewport;
        }
    }

    private void LateUpdate()
    {
        if (viewport == null || content == null) return;

        Vector2 pos = content.anchoredPosition;
        pos.x = Mathf.Clamp(pos.x, -maxOffsetX, maxOffsetX);
        pos.y = Mathf.Clamp(pos.y, -maxOffsetY, maxOffsetY);
        content.anchoredPosition = pos;
    }

    private void OnDrawGizmosSelected()
    {
        if (viewport == null) return;

        Vector3 center = viewport.TransformPoint(viewport.rect.center);
        Vector3 size = new Vector3(maxOffsetX * 2, maxOffsetY * 2, 0);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(center, size);
    }
}