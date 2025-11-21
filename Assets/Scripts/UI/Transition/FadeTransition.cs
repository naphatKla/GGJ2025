using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace UI.Transition
{
    [CreateAssetMenu(fileName = "FadeTransition", menuName = "UI/Transitions/FadeTransition")]
    public class FadeTransition : TransitionBase
    {
        [SerializeField] private float duration = 0.3f;
        [SerializeField] private Ease ease = Ease.InOutQuad;
        
        public override async UniTask PlayAsync(GameObject target, bool isAppear, CancellationToken cts)
        {
            if (!target) return;
            var canvasGroup = target.GetComponent<CanvasGroup>();
            if (!canvasGroup)
            {
                canvasGroup = target.AddComponent<CanvasGroup>();
            }
            
            DOTween.Kill(canvasGroup, complete: false);

            float from = isAppear ? 0f : 1f;
            float to   = isAppear ? 1f : 0f;

            canvasGroup.alpha = from;

            try
            {
                await canvasGroup
                    .DOFade(to, duration)
                    .SetEase(ease)
                    .SetUpdate(true)
                    .ToUniTask(cancellationToken: cts);
            }
            catch (OperationCanceledException)
            {
                canvasGroup.alpha = to;
            }
        }
    }
}