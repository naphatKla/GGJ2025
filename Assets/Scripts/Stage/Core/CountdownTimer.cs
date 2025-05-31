using System.Threading;
using UnityEngine;
using TMPro;
using DG.Tweening;
using Cysharp.Threading.Tasks;

public class CountdownTimer : MonoBehaviour
{
    public TMP_Text countdownText;
    private CancellationTokenSource cts;

    /// <summary>
    /// Countdown timer with Unitask
    /// </summary>
    /// <param name="seconds">time to countdown in seconds</param>
    public async UniTask StartCountdownAsync(float seconds)
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();

        countdownText.gameObject.SetActive(true);

        while (seconds > 0f)
        {
            int displayNum = Mathf.CeilToInt(seconds);
            countdownText.text = displayNum.ToString();

            // Scale effect
            countdownText.transform.localScale = Vector3.one * 2.5f;
            countdownText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);

            await UniTask.Delay(1000, cancellationToken: cts.Token);
            seconds -= 1f;
        }
        
        countdownText.text = "Start!";
        countdownText.transform.localScale = Vector3.one * 3f;
        countdownText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);

        await UniTask.Delay(1000, cancellationToken: cts.Token);

        countdownText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        cts?.Cancel();
    }
}