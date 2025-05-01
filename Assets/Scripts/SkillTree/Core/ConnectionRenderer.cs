using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;

public class ConnectionRenderer : MonoBehaviour
{
    [SerializeField] private RectTransform content;
    private Canvas canvas;
    private float lastContentScale = 1f;
    private readonly List<UILineRenderer> uiLineRenderers = new();
    private GameObject lineParent;

    /// <summary>
    /// Caches the Canvas component from the parent of the content RectTransform.
    /// </summary>
    private void Awake()
    {
        canvas = content.GetComponentInParent<Canvas>();
        lineParent = new GameObject("LineParent");
        lineParent.transform.SetParent(content, false);
    }

    /// <summary>
    /// Draws a line between two RectTransforms with specified color and thickness.
    /// </summary>
    public void DrawLine(RectTransform startRect, RectTransform endRect, Color color, float baseThickness = 10f)
    {
        var lineObj = new GameObject("UIConnectionLine", typeof(RectTransform), typeof(UILineRenderer), typeof(Canvas));
        lineObj.transform.SetParent(lineParent.transform, false);

        var rect = lineObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        var uiLine = lineObj.GetComponent<UILineRenderer>();
        Vector2 startPos = content.InverseTransformPoint(startRect.position);
        Vector2 endPos = content.InverseTransformPoint(endRect.position);

        uiLine.color = color;
        uiLine.LineThickness = baseThickness / content.localScale.x;
        uiLine.LineList = false;
        uiLine.Points = new[] { startPos, endPos };
        uiLine.material = new Material(Shader.Find("UI/Default"));

        var lineCanvas = lineObj.GetComponent<Canvas>();
        lineCanvas.overrideSorting = true;
        lineCanvas.sortingOrder = -1;

        uiLineRenderers.Add(uiLine);
    }

    /// <summary>
    /// Clears all the currently drawn lines from the UI.
    /// </summary>
    public void ClearLines()
    {
        foreach (var line in uiLineRenderers)
            if (line != null)
                Destroy(line.gameObject);

        uiLineRenderers.Clear();
    }

    /// <summary>
    /// Updates the thickness of all drawn lines based on the current content scale.
    /// </summary>
    public void UpdateLineScales()
    {
        if (Mathf.Approximately(content.localScale.x, lastContentScale)) return;

        var inverseScale = 1f / content.localScale.x;
        foreach (var line in uiLineRenderers)
            if (line != null)
                line.LineThickness = 10f * inverseScale;

        lastContentScale = content.localScale.x;
    }
}