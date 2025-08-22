using DG.Tweening;
using PixelUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.IngameViewholder
{
    public class StatusSlotViewholder : MonoBehaviour
    {
        [SerializeField] public Image buffIcon;
        [SerializeField] public ValueBar valueBar;
        [SerializeField] public TMP_Text durationText;
        [SerializeField] public Image statusFrame;
        [SerializeField] public CanvasGroup canvasGroup;
        private Tween _openTween;
        private Tween _closeTween;

        public void Show(bool value)
        {
            if (value)
            {
                if (gameObject.activeInHierarchy) return;
                if (_openTween.IsActive()) return;
                
                canvasGroup.alpha = 0;
                gameObject.SetActive(true);
                _closeTween?.Kill();
                _openTween = canvasGroup.DOFade(1f, 0.15f);
                return;
            }
            
            if (!gameObject.activeInHierarchy) return;
            if (_closeTween.IsActive()) return;
            
            _openTween.Kill();
            _closeTween = canvasGroup.DOFade(0f, 0.15f).OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
        }
    }
}
