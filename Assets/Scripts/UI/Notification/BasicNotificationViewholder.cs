using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace UI.Notification
{
    public class BasicNotificationViewholder : NotificationViewholderBase
    {
        [Header("Anim (Slide In)")]
        [SerializeField] private float slideOffset = 240f;
        [SerializeField] private Ease  slideEase   = Ease.OutCubic;
        
        /// <summary>default play in (slide from left)</summary>
        public override async UniTask PlayInAsync(float dur = .15f, float scalePunch = .05f)
        {
            await EnsureLayoutReadyAsync();
            DOTween.Kill(cg, true);
            cg.alpha = 0f;
            var orig = rt.anchoredPosition;
            var offset = slideOffset > 0f ? slideOffset : Mathf.Max(rt.rect.width, 300f);
            
            rt.anchoredPosition = orig + new Vector2(-offset, 0f);
            rt.localScale = Vector3.one * (1f + scalePunch);
            
            var seq = DOTween.Sequence()
                .Join(cg.DOFade(1f, dur))
                .Join(rt.DOAnchorPosX(orig.x, dur).SetEase(slideEase))
                .Join(rt.DOScale(1f, dur))
                .SetUpdate(true)
                .SetLink(gameObject);
            
            await seq.AsyncWaitForCompletion();
        }
        
        /// <summary>default play bump on same id</summary>
        public override async UniTask PlayBumpAsync(float dur = .08f, float amount = .05f)
        {
            CountTextFeedback(numText);
            var tw = rt.DOPunchScale(Vector3.one * amount, dur, vibrato: 1, elasticity: 0.5f)
                .SetUpdate(true)
                .SetLink(gameObject);

            await tw.AsyncWaitForCompletion();
        }

        private void CountTextFeedback(TMP_Text text)
        {
            numText.transform.DOPunchScale(Vector3.one * 0.4f, 0.25f, vibrato: 3, elasticity: 0.8f).SetUpdate(true).SetLink(gameObject);
        }

    }
}
