using System;
using System.Collections.Generic;
using Characters.SO.CharacterDataSO;
using Coffee.UIEffects;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.IngameViewholder
{
    public class CombatRankViewHolder : MonoBehaviour
    {
        [Title("Grade Combo")]
        public Image gradeImage;
        public Image gradeFlame;
        public TMP_Text gradeText;
        public Image rankpointBarFill;
        public GameObject thunderObj;
        public UIEffect thunderColor;

        [Serializable]
        public struct GradeCombo
        {
            public string gradeId;
            public string supText;
            public Sprite gradeImage;
            public Color colorFill;
            public bool alwayShowflame;
            public bool showthunder;
        }
        
        public List<GradeCombo> gradeComboList;
        
        public void UpdateGradeCombo(CombatRankData oldRank, CombatRankData newRank)
        {
            string rankId = newRank.rankId;
            gradeImage.gameObject.SetActive(rankId != null);
            gradeFlame.gameObject.SetActive(rankId != null);
            gradeText.gameObject.SetActive(rankId != null);
            foreach (var g in gradeComboList)
                if (g.gradeId == rankId)
                {
                    rankpointBarFill.color = g.colorFill;
                    GradeFeedback(gradeImage, g.gradeImage, g.alwayShowflame);
                    ThunderFeedback(g.colorFill, g.showthunder);
                    GradeTextFeedBack(gradeText, g.supText);
                    break;
                }
        }

        private void GradeFeedback(Image obj, Sprite newSprite, bool showFlame)
        {
            var tf = obj.transform;
            var cg = obj.GetComponent<CanvasGroup>();
            if (cg == null) cg = obj.gameObject.AddComponent<CanvasGroup>();

            var flameCg = gradeFlame.GetComponent<CanvasGroup>();
            if (flameCg == null) flameCg = gradeFlame.gameObject.AddComponent<CanvasGroup>();

            flameCg.DOKill();
            gradeFlame.transform.DOKill();
    
            flameCg.alpha = 1f;
            gradeFlame.transform.localScale = Vector3.one;
            gradeFlame.gameObject.SetActive(true);

            var sq = DOTween.Sequence().SetUpdate(true);

            sq.Append(cg.DOFade(0f, 0.15f).SetUpdate(true))
                .AppendCallback(() => { obj.sprite = newSprite; })
                .Append(cg.DOFade(1f, 0.25f).SetUpdate(true))
                .Join(tf.DOScale(1.6f, 0.25f).SetEase(Ease.OutBack).SetUpdate(true))
                .Append(tf.DOScale(1f, 0.15f).SetEase(Ease.InBack).SetUpdate(true))
                .AppendInterval(1.5f)
                .AppendCallback(() =>
                {
                    if (showFlame) return;
                    flameCg.DOFade(0f, 0.5f).SetUpdate(true)
                        .OnComplete(() => { gradeFlame.gameObject.SetActive(false); });
                });
        }
        
        private void ThunderFeedback(Color color, bool showThunder)
        {
            if (showThunder)
            {
                thunderObj.gameObject.SetActive(true);
                thunderColor.color = color;
            }
            else
            {
                thunderObj.gameObject.SetActive(false);
            }
        }

        private void GradeTextFeedBack(TMP_Text obj, string newText)
        {
            obj.DOKill();
            
            var cg = obj.GetComponent<CanvasGroup>();
            if (cg == null) cg = obj.gameObject.AddComponent<CanvasGroup>();
            cg.DOKill();
            cg.alpha = 0f; 

            obj.text = newText;
            
            var rt = obj.rectTransform;
            rt.anchoredPosition = new Vector2(400f, rt.anchoredPosition.y);

            // Tween Sequence
            var seq = DOTween.Sequence();
            seq.Append(cg.DOFade(1f, 0.3f)) // Fade in
                .Join(rt.DOAnchorPosX(0f, 0.5f).SetEase(Ease.OutBack)) // เลื่อนเข้ากลาง
                .AppendInterval(1.2f)
                .Append(cg.DOFade(0f, 0.5f)) // Fade out
                .Join(rt.DOAnchorPosX(400f, 0.5f).SetEase(Ease.InBack)); // เลื่อนออกซ้าย
        }

    }
}
