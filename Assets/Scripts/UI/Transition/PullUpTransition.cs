using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace UI.Transition
{
    [CreateAssetMenu(fileName = "PullupTransition", menuName = "UI/Transitions/PullupTransition")]
    public class PullUpTransition : TransitionBase
    {
        [SerializeField] private Vector3 startPosition = new Vector3(50 ,50 ,1);
        [SerializeField] private Vector3 targetPosition = new Vector3(50 ,50 ,1);
        [SerializeField] private float delayPulldown = 1f;
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
            
            var t = target.transform;
           
            var seq = DOTween.Sequence();
            seq.Append(canvasGroup.DOFade(targetAlpha, 0.15f).SetEase(ease))
                .Join(t.DOLocalMove(targetPosition, duration).SetEase(ease))
                .AppendInterval(delayPulldown)
                .Append(t.DOLocalMove(startPosition, duration).SetEase(ease))
                .Join(canvasGroup.DOFade(0, 0.15f).SetEase(ease))
                .ToUniTask(cancellationToken: cts);
        }
    }

}
