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
    [SerializeField] private float startY = -600f;    // จุดเริ่ม Y (ต่ำกว่าหน้าจอ)
    [SerializeField] private float endY =  800f;      // จุดจบ Y (สูงกว่าหน้าจอ)
    [SerializeField] private float duration = 20f;    // ระยะเวลาสไลด์ทั้งช่วง (วินาที)
    [SerializeField] private AnimationCurve ease = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Return / Skip")]
    [SerializeField] private string mainSceneName = "Main";
    [SerializeField] private float skipEnableDelay = 1.0f;  // หน่วงก่อนกดข้ามได้ กันเผลอคลิก
    [SerializeField] private string hintMessage = "Click or tap to skip...";

    private bool _returning;
    private bool _skipReady;

    private void OnEnable()
    {
        if (hintText != null)
        {
            hintText.text = hintMessage;
            StartCoroutine(BlinkHint());
        }

        StartCoroutine(EnableSkipAfterDelay());
        StartCoroutine(PlayCredits());
    }

    private void Update()
    {
        if (_returning || !_skipReady) return;

        // คลิกเมาส์ / แตะจอ / กดปุ่มใดๆ เพื่อข้าม
        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0 || Input.anyKeyDown)
        {
            ReturnToMain();
        }
    }

    private IEnumerator PlayCredits()
    {
        if (content == null)
            yield break;

        // ตั้งค่าเริ่ม
        var startPos = content.anchoredPosition;
        startPos.y = startY;
        content.anchoredPosition = startPos;

        float t = 0f;
        while (t < duration && !_returning)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = ease.Evaluate(p);

            content.anchoredPosition = new Vector2(
                startPos.x,
                Mathf.LerpUnclamped(startY, endY, eased)
            );

            yield return null;

            // เผื่อกดข้ามระหว่างทาง
            if (_returning) yield break;
        }

        ReturnToMain();
    }

    private IEnumerator BlinkHint()
    {
        // กระพริบด้วยการปรับ alpha 0–1 แบบ sine
        var c = hintText.color;
        while (!_returning)
        {
            float a = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f); // 2Hz
            hintText.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }
    }

    private IEnumerator EnableSkipAfterDelay()
    {
        _skipReady = false;
        yield return new WaitForSeconds(skipEnableDelay);
        _skipReady = true;
    }

    public void Skip() => ReturnToMain();

    private void ReturnToMain()
    {
        if (_returning) return;
        _returning = true;
        SceneManager.LoadScene(mainSceneName);
    }
}
