using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace UI.Transition
{
    [CreateAssetMenu(fileName = "PullupTransition", menuName = "UI/Transitions/PullupTransition")]
    public class PullUpTransition : TransitionBase
    {
        [SerializeField] private Vector3 targetPosition = new Vector3(50 ,50 ,1);
        [SerializeField] private float delayPulldown = 1f;
        [SerializeField] private float duration = 0.3f;
        [SerializeField] private Ease ease = Ease.InOutQuad;
        
        public override async UniTask PlayAsync(GameObject target, bool isAppear, CancellationToken cts)
        {
            if (target == null) return;

            var canvasGroup = target.GetComponent<CanvasGroup>() ?? target.AddComponent<CanvasGroup>();
            DOTween.Kill(canvasGroup, complete: true);

            var t = target.transform;
            var seq = DOTween.Sequence()
                .Append(t.DOLocalMove(targetPosition, duration).SetEase(ease))
                .AppendInterval(delayPulldown);

            await seq.ToUniTask(cancellationToken: cts);
        }
    }

}
