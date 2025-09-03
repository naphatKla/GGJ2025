using Characters.HeathSystems;
using DG.Tweening;
using UnityEngine;

namespace Characters.FeedbackSystems.NonMMFeedback
{
    public class PlayerLowHpFeedback : MonoBehaviour
    {
        [SerializeField] private HealthSystem ownerHealth;
        [SerializeField] private CanvasGroup hpCanvas;
        [SerializeField] private float canvasActivateOnHpPercent01 = 0.5f;
        [SerializeField] private float fadeDuration = 0.15f;
        [SerializeField] private float breathDuration = 0.5f;
        [SerializeField] private float shakeAlphaRange = 0.3f;
        private Tween fadeAlphaTween;
        private Tween shakeAlphaTween;

        private void OnEnable()
        {
            ownerHealth.OnHealthChange += UpdateCanvasAlpha;
        }

        private void OnDisable()
        {
            ownerHealth.OnHealthChange -= UpdateCanvasAlpha;
        }

        private void UpdateCanvasAlpha(float healthChanged)
        {
            if (ownerHealth.HealthPercentage01 > canvasActivateOnHpPercent01)
            {
                StopShakeAlpha();
                hpCanvas.alpha = 0;
                return;
            }

            StopShakeAlpha();
            float desireAlpha = (canvasActivateOnHpPercent01 - ownerHealth.HealthPercentage01) / canvasActivateOnHpPercent01;
            PlayShakeAlpha(desireAlpha, shakeAlphaRange);
        }

        private void PlayShakeAlpha(float desireAlpha, float shakeRange)
        {
            fadeAlphaTween = hpCanvas.DOFade(desireAlpha, fadeDuration).OnComplete(() =>
            {
                float shakeAlpha = Mathf.Clamp01(desireAlpha - shakeAlphaRange);
                shakeAlphaTween = hpCanvas.DOFade(shakeAlpha, breathDuration).SetLoops(-1, LoopType.Yoyo);
            });
        }

        private void StopShakeAlpha()
        {
            fadeAlphaTween?.Kill(true);
            shakeAlphaTween?.Kill(true);
        }
    }
}
