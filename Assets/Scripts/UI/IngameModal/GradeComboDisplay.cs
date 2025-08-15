using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI.IngameModal
{
    public class GradeComboDisplay : MonoBehaviour
    {
        [Title("Grade Combo")] [FoldoutGroup("Combo Display")]
        public Image gradeImage;
        
        [Serializable]
        public struct GradeCombo { public string gradeId; public Sprite gradeImage; }
           
        [FoldoutGroup("Combo Display")]
        public List<GradeCombo> gradeComboList;
        
        public void UpdateGradeCombo(string grade)
        {
            gradeImage.gameObject.SetActive(grade != null);
            foreach (var g in gradeComboList)
                if (g.gradeId == grade)
                {
                    GradeFeedback(gradeImage, g.gradeImage);
                    break;
                }
            
        }

        private void GradeFeedback(Image obj, Sprite newSprite)
        {
            var tf = obj.transform;
            var cg = obj.GetComponent<CanvasGroup>();
            if (cg == null) cg = obj.gameObject.AddComponent<CanvasGroup>();

            var sq = DOTween.Sequence();

            sq.Append(cg.DOFade(0f, 0.15f))
                .AppendCallback(() =>
                {
                    obj.sprite = newSprite;
                })
                .Append(cg.DOFade(1f, 0.25f))
                .Join(tf.DOScale(1.6f, 0.25f).SetEase(Ease.OutBack))
                .Append(tf.DOScale(1f, 0.15f).SetEase(Ease.InBack));
        }
    }
}
