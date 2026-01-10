using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Milestone
{
    public class MilestonePathRenderer : MonoBehaviour
    {
        [Header("Draw Space")]
        [SerializeField] private RectTransform lineRoot;

        [Header("Prefab")]
        [SerializeField] private Image segmentPrefab;

        [Header("Style")]
        [SerializeField] private float thickness = 8f;

        private readonly Dictionary<int, RectTransform> _nodeByIndex = new();
        private readonly List<Image> _segments = new();

        private int _selectedIndex = -1;
        private Canvas _canvas;
        
        private void Awake()
        {
            _canvas = lineRoot != null ? lineRoot.GetComponentInParent<Canvas>() : null;
        }

        public void SetSelectedIndex(int selectedIndex)
        {
            _selectedIndex = selectedIndex;
            Redraw();
        }

        public void RegisterNode(int index, RectTransform nodePoint)
        {
            if (nodePoint == null) return;
            _nodeByIndex[index] = nodePoint;
            Redraw();
        }

        public void UnregisterNode(int index, RectTransform nodePoint)
        {
            // กันเคส recycle: ลบเฉพาะอันเดียวกันจริงๆ
            if (_nodeByIndex.TryGetValue(index, out var rt) && rt == nodePoint)
                _nodeByIndex.Remove(index);

            Redraw();
        }

        private void EnsureSegments(int count)
        {
            while (_segments.Count < count)
            {
                var img = Instantiate(segmentPrefab, lineRoot);
                img.gameObject.SetActive(true);
                _segments.Add(img);
            }

            for (int i = 0; i < _segments.Count; i++)
                _segments[i].gameObject.SetActive(i < count);
        }

        private Vector2 ToLocal(RectTransform target)
        {
            Camera cam = null;

            if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = _canvas.worldCamera;

            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, target.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(lineRoot, screen, cam, out var local);
            return local;
        }

        public void Redraw()
        {
            if (lineRoot == null || segmentPrefab == null) return;
            if (_selectedIndex <= 0) { EnsureSegments(0); return; }

            int need = 0;
            for (int i = 0; i < _selectedIndex; i++)
            {
                if (!_nodeByIndex.TryGetValue(i, out var a)) continue;
                if (!_nodeByIndex.TryGetValue(i + 1, out var b)) continue;

                need++;
            }

            EnsureSegments(need);

            int segIdx = 0;
            for (int i = 0; i < _selectedIndex; i++)
            {
                if (!_nodeByIndex.TryGetValue(i, out var a)) continue;
                if (!_nodeByIndex.TryGetValue(i + 1, out var b)) continue;

                Vector2 p1 = ToLocal(a);
                Vector2 p2 = ToLocal(b);

                Vector2 dir = (p2 - p1);
                float len = dir.magnitude;
                if (len <= 0.001f) continue;

                var img = _segments[segIdx++];
                var rt = img.rectTransform;

                rt.anchoredPosition = (p1 + p2) * 0.5f;
                rt.sizeDelta = new Vector2(thickness, len);
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                rt.localRotation = Quaternion.Euler(0, 0, angle);
            }
            for (int i = segIdx; i < _segments.Count; i++)
                _segments[i].gameObject.SetActive(false);
        }
    }
}
