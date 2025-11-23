using System;
using Coffee.UIEffects;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UISelectModeController : MonoBehaviour
    {
        [Serializable]
        public class ModeItem
        {
            [Tooltip("ชื่อโหมดไว้ดูใน Inspector เฉยๆ")]
            public string modeName;

            [Tooltip("ปุ่มที่ใช้เลือกโหมดนี้")]
            public Button button;

            [Tooltip("Panel / GameObject ที่ต้องเปิดเมื่อเลือกโหมดนี้")]
            public GameObject contentRoot;

            [Tooltip("Feedback ตอนโหมดนี้ถูก Select (เช่น กรอบ, Highlight, Glow ฯลฯ)")]
            public UIEffect selectedFeedback;
        }

        [Header("ตั้งค่าโหมดต่าง ๆ")]
        public ModeItem[] modes;

        [Header("ค่าเริ่มต้นตอนเปิดหน้าจอ")]
        [Tooltip("เริ่มที่ index ไหนของ modes (0 = ตัวแรก)")]
        public int defaultIndex;

        private int _currentIndex = -1;

        [Header("Panel Feedback (Hytech Sci-Fi)")]
        [SerializeField] public GameObject panelFeedback;

        [SerializeField] private float offscreenPadding = 200f;
        [SerializeField] private float exitDuration = 0.12f;
        [SerializeField] private float enterDuration = 0.30f;
        [SerializeField] private float pauseDuration = 0.03f;
        [SerializeField] private Ease exitEase = Ease.InQuad;
        [SerializeField] private Ease enterEase = Ease.OutExpo;
        [SerializeField] private bool punchScaleOnEnter = true;
        [SerializeField] private float punchStrength = 0.08f;
        [SerializeField] private float punchDuration = 0.25f;

        private RectTransform _panelRect;
        private RectTransform _canvasRect;
        private Vector2 _panelOriginalPos;
        private Vector3 _panelOriginalScale;

        private void Awake()
        {
            // ผูกปุ่มทุกอันให้เรียก SelectMode(index)
            for (var i = 0; i < modes.Length; i++)
            {
                var index = i; // เก็บไว้ใน local เพื่อใช้ใน lambda
                if (modes[i].button != null)
                    modes[i].button.onClick.AddListener(() => SelectMode(index));
            }

            // เตรียมค่าเริ่มต้นของ panelFeedback สำหรับเล่น DoTween
            if (panelFeedback != null)
            {
                _panelRect = panelFeedback.GetComponent<RectTransform>();
                if (_panelRect != null)
                {
                    _panelOriginalPos = _panelRect.anchoredPosition;
                    _panelOriginalScale = _panelRect.localScale;

                    var canvas = _panelRect.GetComponentInParent<Canvas>();
                    if (canvas != null)
                        _canvasRect = canvas.GetComponent<RectTransform>();
                }
            }
        }

        private void OnEnable()
        {
            // เลือกค่าเริ่มต้นตอนเปิด
            if (modes != null && modes.Length > 0)
            {
                defaultIndex = Mathf.Clamp(defaultIndex, 0, modes.Length - 1);
                SelectMode(defaultIndex);
            }
        }

        /// <summary>
        ///     เรียกเพื่อเปลี่ยนโหมด ตาม index ใน Array modes
        /// </summary>
        public void SelectMode(int index)
        {
            if (modes == null || modes.Length == 0)
                return;

            if (index < 0 || index >= modes.Length)
                return;

            _currentIndex = index;

            for (var i = 0; i < modes.Length; i++)
            {
                var item = modes[i];

                // เปิด/ปิด Panel ตามโหมด
                if (item.contentRoot != null)
                    item.contentRoot.SetActive(i == index);

                // เปิด/ปิด Feedback ปุ่ม (เช่น กรอบ, Glow, ขยาย scale ฯลฯ)
                if (item.selectedFeedback != null)
                {
                    if (i == index)
                    {
                        item.selectedFeedback.edgeMode = EdgeMode.Shiny;
                        item.selectedFeedback.transitionFilter = TransitionFilter.Pattern;
                    }
                    else
                    {
                        item.selectedFeedback.edgeMode = EdgeMode.None;
                        item.selectedFeedback.transitionFilter = TransitionFilter.None;
                    }
                }
            }

            // เล่นเอฟเฟกต์ Panel เวลาเปลี่ยนโหมด
            //PlayerPanelFeedbackOnSelect();
        }

        /// <summary>
        ///     ถ้าจะเรียกจาก Animator / EventSystem โดยใช้ชื่อโหมด
        /// </summary>
        public void SelectModeByName(string modeName)
        {
            if (string.IsNullOrEmpty(modeName) || modes == null) return;

            for (var i = 0; i < modes.Length; i++)
            {
                if (string.Equals(modes[i].modeName, modeName, StringComparison.OrdinalIgnoreCase))
                {
                    SelectMode(i);
                    return;
                }
            }
        }

        /// <summary>
        /// เอฟเฟกต์ Panel แบบ Hytech: เลื่อนออกซ้าย → วาร์ปไปขวา → บินกลับเข้ามา
        /// </summary>
        public void PlayerPanelFeedbackOnSelect()
        {
            if (_panelRect == null)
                return;

            // กัน Tween ค้าง
            DOTween.Kill(_panelRect);

            float width = _canvasRect != null ? _canvasRect.rect.width : Screen.width;

            float leftX = _panelOriginalPos.x - width - offscreenPadding;
            float rightX = _panelOriginalPos.x + width + offscreenPadding;

            _panelRect.localScale = _panelOriginalScale;

            var seq = DOTween.Sequence();
            seq.SetTarget(_panelRect);

            // 1) สไลด์ออกซ้ายเร็ว ๆ
            seq.Append(
                _panelRect.DOAnchorPosX(leftX, exitDuration)
                    .SetEase(exitEase)
            );

            // 2) วาร์ปไปฝั่งขวานอกจอ
            seq.AppendCallback(() =>
            {
                _panelRect.anchoredPosition = new Vector2(rightX, _panelOriginalPos.y);
            });

            // 3) หน่วงนิดนึงให้ดูเหมือนระบบประมวลผล
            if (pauseDuration > 0f)
                seq.AppendInterval(pauseDuration);

            // 4) บินกลับเข้ามาที่ตำแหน่งเดิมจากฝั่งขวา
            seq.Append(
                _panelRect.DOAnchorPosX(_panelOriginalPos.x, enterDuration)
                    .SetEase(enterEase)
            );

            // 5) ดีด scale เล็กน้อยให้ฟีล HUD Sci-Fi
            if (punchScaleOnEnter)
            {
                seq.Join(
                    _panelRect.DOPunchScale(
                        Vector3.one * punchStrength,
                        punchDuration,
                        2,
                        0.5f
                    )
                );
            }
        }
    }
}
