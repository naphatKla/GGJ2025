using Challenge;
using Coffee.UIEffects;
using Demo;
using DG.Tweening;
using Player;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace UI.Milestone
{
    public class MilestoneViewholder : ScrollIndexCallbackBase
    {
        [Title("Viewholder")] 
        public TMP_Text levelVh;
        public Image imageVh;
        
        [Title("Display")] 
        public UIEffect uiEffect;
        public Gradient selectGradient;
        
        [Title("Unlock Display")] 
        public Color unlockColor;
        
        [Title("Lock Display")]
        public Color lockColor;

        [Title("Selected Display")]
        [Tooltip("ขนาดตอนถูกเลือก (ปกติ = 1)")]
        public float selectedScale = 1.3f;
        [Tooltip("ขยายเพิ่มตอนเต้น (Pulse) ตอนถูกเลือก, 0 = ไม่เต้น")]
        public float selectedPulse = 0.12f;
        public float pulseDuration = 0.6f;
        public Color selectedTextColor = new Color(1f, 0.84f, 0f, 1f);
        [Tooltip("สีเส้นขอบ (Outline) ตอนถูกเลือก")]
        public Color selectedOutlineColor = new Color(1f, 0.84f, 0f, 1f);
        [Tooltip("ให้เส้นขอบเรืองแสง (Glow)")]
        public bool selectedOutlineGlow;
        [Tooltip("ตัวบอกว่าเลือกอยู่ (เช่นลูกศร ▼) เปิดเฉพาะตอนถูกเลือก")]
        public RectTransform selectedMarker;
        public float markerBob = 4f;

        private bool _isSelected;
        private Color _defaultTextColor = Color.white;
        private bool _hasDefaultTextColor;
        private Vector2 _markerBasePos;
        private bool _hasMarkerBasePos;
        private Vector2 _textBasePos;
        private bool _hasTextBasePos;

        [Title("Node")]
        [SerializeField] private RectTransform nodePoint;
        private MilestonePathRenderer _path;
        private int _index = -1;

        public void BindPath(MilestonePathRenderer path, int idx)
        {
            if (_path != null && nodePoint != null && _index >= 0)
                _path.UnregisterNode(_index, nodePoint);

            _path = path;
            _index = idx;

            if (_path != null && nodePoint != null && _index >= 0)
                _path.RegisterNode(_index, nodePoint);
        }

        private void OnDisable()
        {
            KillSelectTweens();
            _isSelected = false;
            transform.localScale = Vector3.one;
            KeepTextUnscaled();

            if (_path != null && nodePoint != null && _index >= 0)
                _path.UnregisterNode(_index, nodePoint);

            _path = null;
            _index = -1;
        }
        
        public void UpdateViewholder(int index, ChallengeDataSO content, MapStat mapStat)
        {
            if (mapStat.MaxLevelUnlockMilestone >= index) //Unlock
            {
                if (imageVh != null) imageVh.color = unlockColor;
                if (levelVh != null) levelVh.text = "LV."+index;
            }
            else
            {
                if (imageVh != null) imageVh.color = lockColor;
                if (levelVh != null) levelVh.text = "LOCKED";
            }
            ApplySelectedText();
        }

        public void SetClickedColor(bool isClicked)
        {
            ApplySelectedState(isClicked);
            if (m_Button == null || m_Button.image == null) return;
            if (isClicked)
            {
                uiEffect.shadowMode = ShadowMode.Outline;
                uiEffect.shadowColor = selectedOutlineColor;
                uiEffect.shadowColorGlow = selectedOutlineGlow;
                ApplyGradient(uiEffect, selectGradient);
            }
            else
            {
                uiEffect.shadowMode = ShadowMode.None;
                uiEffect.gradationMode = GradationMode.None;
            }
        }
        
        /// <summary>ตัวที่ถูกเลือก: ขยาย + เต้น, ตัวอักษรสีทอง, โชว์ Marker ลอยขึ้นลง</summary>
        private void ApplySelectedState(bool selected)
        {
            bool changed = _isSelected != selected;
            _isSelected = selected;
            ApplySelectedText();
            if (!changed && (!selected || DOTween.IsTweening(transform))) return;

            KillSelectTweens();
            transform.localScale = Vector3.one * (selected ? selectedScale : 1f);
            KeepTextUnscaled();

            if (selectedMarker != null)
            {
                if (!_hasMarkerBasePos)
                {
                    _markerBasePos = selectedMarker.anchoredPosition;
                    _hasMarkerBasePos = true;
                }
                selectedMarker.anchoredPosition = _markerBasePos;
                selectedMarker.gameObject.SetActive(selected);
            }

            if (!selected || !isActiveAndEnabled) return;

            if (selectedPulse > 0f)
                transform.DOScale(selectedScale + selectedPulse, pulseDuration)
                    .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true)
                    .SetLink(gameObject)
                    .OnUpdate(KeepTextUnscaled);

            if (selectedMarker != null && markerBob > 0f)
                selectedMarker.DOAnchorPosY(_markerBasePos.y + markerBob, pulseDuration)
                    .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true)
                    .SetLink(gameObject);
        }

        private void ApplySelectedText()
        {
            if (levelVh == null) return;
            if (!_hasDefaultTextColor)
            {
                _defaultTextColor = levelVh.color;
                _hasDefaultTextColor = true;
            }
            levelVh.color = _isSelected ? selectedTextColor : _defaultTextColor;
            levelVh.fontStyle = _isSelected ? FontStyles.Bold : FontStyles.Normal;
        }

        /// <summary>ขยายแค่ตัวเพชร - ตัวหนังสือ LV.x คงขนาด/ตำแหน่งเดิม ไม่ล้นไปทับ UI ข้างล่าง</summary>
        private void KeepTextUnscaled()
        {
            if (levelVh == null) return;
            var rt = levelVh.rectTransform;
            if (!_hasTextBasePos)
            {
                _textBasePos = rt.anchoredPosition;
                _hasTextBasePos = true;
            }
            float s = Mathf.Max(0.01f, transform.localScale.x);
            rt.localScale = Vector3.one / s;
            rt.anchoredPosition = _textBasePos / s;
        }

        private void KillSelectTweens()
        {
            transform.DOKill();
            if (selectedMarker != null) selectedMarker.DOKill();
        }

        private void ApplyGradient(UIEffect effect, Gradient gradient)
        {
            if (!effect || gradient == null) return;
            effect.gradationMode = GradationMode.AngleGradient;
            effect.gradationIntensity = 1f;
            effect.SetGradientKeys(
                gradient.colorKeys,
                gradient.alphaKeys,
                gradient.mode
            );
        }
    }
}
