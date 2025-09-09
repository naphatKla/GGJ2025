using System;
using System.Collections;
using System.Collections.Generic;
using Characters.SO.SkillDataSo;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.IngameViewholder
{
    public class SolfUpgradeViewholder : MonoBehaviour
    {
        [SerializeField] private TMP_Text skillTitle;
        [SerializeField] private Image skillIcon;
        [SerializeField] private TMP_Text skillDescription;
        [SerializeField] private GameObject selectEffect;
        [SerializeField] private TMP_Text newSkillText;
        [SerializeField] private Button raycast;

        private Vector3 _startSize;
        public BaseSkillDataSo Data { get; private set; }
        public bool IsSelected { get; private set; }

        public event Action<SolfUpgradeViewholder> Clicked;

        public void Bind(BaseSkillDataSo data, bool isNew, Action<SolfUpgradeViewholder> onClick)
        {
            Data = data;
            UpdateUIModal(data, isNew);
            raycast.onClick.RemoveAllListeners();
            Clicked = null;
            Clicked += onClick;
            raycast.onClick.AddListener(() => Clicked?.Invoke(this));
            _startSize = transform.localScale;
            
            SetSelected(false, instant: true);
        }
        
        public void SetSelected(bool selected, bool instant = false)
        {
            IsSelected = selected;
            selectEffect.SetActive(selected);
            var t = transform;
            t.localScale = _startSize;

            if (selected && !instant)
            {
                t.DOKill();
                var seq = DOTween.Sequence();
                seq.Append(t.DOScale(new Vector3(6.45f, 6.45f, 1f), 0.2f).SetEase(Ease.InOutSine))
                    .Append(t.DOShakePosition(0.2f, 10f, vibrato: 10, randomness: 40))
                    .SetUpdate(true);
            }
            else
            {
                t.DOKill();
                var seq = DOTween.Sequence();
                seq.Append(t.DOLocalRotate(new Vector3(0, 360f, 0), 0.3f, RotateMode.FastBeyond360))
                    .Append(t.DOScale(new Vector3(6f, 6f, 1f), 0.2f).SetEase(Ease.InOutSine))
                    .SetUpdate(true);
            }
        }

        public void UpdateUIModal(BaseSkillDataSo data, bool isNew)
        {
            skillTitle.text = data.SkillName + $" <color=yellow>Lv.{data.Level}";
            skillIcon.sprite = data.SkillIcon;
            skillDescription.text = data.SkillDescription;
            
            if (isNew) NewSkillTextFeedBack(newSkillText);
            else newSkillText.gameObject.SetActive(false);
        }
        
        private void NewSkillTextFeedBack(TMP_Text obj)
        {
            newSkillText.gameObject.SetActive(true);
            
            obj.DOKill();
            var cg = obj.GetComponent<CanvasGroup>();
            if (cg == null) cg = obj.gameObject.AddComponent<CanvasGroup>();
            cg.DOKill();
            cg.alpha = 0f; 
    
            var rt = obj.rectTransform;
            rt.anchoredPosition = new Vector2(400f, rt.anchoredPosition.y);
            
            var seq = DOTween.Sequence();

            seq.AppendInterval(0.8f).SetUpdate(true)
                .Append(cg.DOFade(1f, 0.3f).SetUpdate(true))
                .Join(rt.DOAnchorPosX(0f, 0.3f).SetEase(Ease.OutCubic).SetUpdate(true));
        }
        
        private void OnDisable()
        {
            transform.DOKill();
            newSkillText.DOKill();
        }
    }
}