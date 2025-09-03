using System.Collections;
using System.Collections.Generic;
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
            if (target == null) return;
            var canvasGroup = target.GetComponent<CanvasGroup>() ?? target.AddComponent<CanvasGroup>();
            DOTween.Kill(canvasGroup, complete: true);
            canvasGroup.alpha = isAppear ? 0f : 1f;
            
            float targetAlpha = isAppear ? 1f : 0f;
            await canvasGroup.DOFade(targetAlpha, duration)
                .SetEase(ease)
                .SetUpdate(true)
                .ToUniTask(cancellationToken: cts);
        }
    }
}
