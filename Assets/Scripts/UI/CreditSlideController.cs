using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditSlideController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform content;
    [SerializeField] private TMP_Text hintText;

    [Header("Slide Settings")]
    [SerializeField] private float startY = -600f;
    [SerializeField] private float endY = 800f;
    [SerializeField] private float duration = 20f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Hold-to-Speed-Up")]
    [SerializeField] private float holdSpeedMultiplier = 3f;   
    [SerializeField] private float holdEnableDelay = 1.0f;     
    [SerializeField] private bool  holdAnyKeyAlsoWorks = true;

    [Header("Return")]
    [SerializeField] private string mainSceneName = "Main";

    [Header("UI")]
    [SerializeField] private string hintMessage = "Hold to speed up...";

    private bool _returning;
    private bool _holdReady;

    private void OnEnable()
    {
        if (hintText != null)
        {
            hintText.text = hintMessage;
            StartCoroutine(BlinkHint());
        }

        StartCoroutine(EnableHoldAfterDelay());
        StartCoroutine(PlayCredits());
    }

    private IEnumerator PlayCredits()
    {
        if (content == null)
            yield break;
        
        var startPos = content.anchoredPosition;
        startPos.y = startY;
        content.anchoredPosition = startPos;

        float t = 0f;
        while (t < duration && !_returning)
        {
            float speedMul = IsHoldActive() ? holdSpeedMultiplier : 1f;
            t += Time.deltaTime * Mathf.Max(0.01f, speedMul);

            float p = Mathf.Clamp01(t / duration);
            float eased = ease.Evaluate(p);

            content.anchoredPosition = new Vector2(
                startPos.x,
                Mathf.LerpUnclamped(startY, endY, eased)
            );

            yield return null;
        }
        
        ReturnToMain();
    }

    private bool IsHoldActive()
    {
        if (!_holdReady) return false;
        
        bool mouseHolding = Input.GetMouseButton(0);
        bool touchHolding = false;
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var phase = Input.GetTouch(i).phase;
                if (phase != TouchPhase.Canceled && phase != TouchPhase.Ended)
                {
                    touchHolding = true;
                    break;
                }
            }
        }
        
        bool keyboardHolding = holdAnyKeyAlsoWorks && Input.anyKey && !Input.GetMouseButton(0);

        return mouseHolding || touchHolding || keyboardHolding;
    }

    private IEnumerator BlinkHint()
    {
        var baseColor = hintText.color;
        while (!_returning)
        {
            float a = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f); // ~2Hz
            hintText.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
            yield return null;
        }
    }

    private IEnumerator EnableHoldAfterDelay()
    {
        _holdReady = false;
        yield return new WaitForSeconds(holdEnableDelay);
        _holdReady = true;
    }

    private void ReturnToMain()
    {
        if (_returning) return;
        _returning = true;
        SceneManager.LoadScene(mainSceneName);
    }
}
