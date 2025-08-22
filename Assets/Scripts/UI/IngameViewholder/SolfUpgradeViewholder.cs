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
        [SerializeField] private Button selectButton;
        [SerializeField] private TMP_Text newSkillText;

        public Button SelectButton => selectButton;

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
    }
}