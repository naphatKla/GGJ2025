using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Notification
{
    public abstract class NotificationViewholderBase : MonoBehaviour
    {
        [Header("Refs")]
        public TMP_Text notifyText;
        public TMP_Text numText;
        [SerializeField] private bool  waitLayoutPass = true;

        protected CanvasGroup cg;
        protected RectTransform rt;

        protected virtual void Awake()
        {
            rt = transform as RectTransform;
            cg = GetComponent<CanvasGroup>();
            if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        /// <summary>Bind first time</summary>
        public virtual void Bind(string text, int count)
        {
            if (notifyText) notifyText.text = text;
            SetCount(count);
        }

        /// <summary>Update count xN</summary>
        public virtual void SetCount(int count)
        {
            if (!numText) return;
            numText.text = (count <= 1) ? "x1" : $"x{count}";
        }

        /// <summary>default play in</summary>
        public virtual async UniTask PlayInAsync(float dur = .15f, float scalePunch = .05f)
        {
            await EnsureLayoutReadyAsync();
            DOTween.Kill(cg, true);
            rt.localScale = Vector3.one * (1f + scalePunch);
            cg.alpha = 0f;

            var seq = DOTween.Sequence()
                .Join(cg.DOFade(1f, dur))
                .Join(rt.DOScale(1f, dur))
                .SetUpdate(true)
                .SetLink(gameObject);

            await seq.AsyncWaitForCompletion();
        }

        /// <summary>default play out</summary>
        public virtual async UniTask PlayOutAsync(float dur = .15f)
        {
            var tw = cg.DOFade(0f, dur)
                .SetUpdate(true)
                .SetLink(gameObject);

            await tw.AsyncWaitForCompletion();
        }

        /// <summary>default play bump on same id</summary>
        public virtual async UniTask PlayBumpAsync(float dur = .08f, float amount = .05f)
        {
            DOTween.Kill(rt, true);
            
            var tw = rt.DOPunchScale(Vector3.one * amount, dur, vibrato: 1, elasticity: 0.5f)
                .SetUpdate(true)
                .SetLink(gameObject);

            await tw.AsyncWaitForCompletion();
        }
        
        public async UniTask EnsureLayoutReadyAsync()
        {
            if (!waitLayoutPass) return;
            Canvas.ForceUpdateCanvases();
            
            var parentRt = (transform.parent as RectTransform);
            if (parentRt) LayoutRebuilder.ForceRebuildLayoutImmediate(parentRt);
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
        }
    }
}
