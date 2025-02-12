using System.Collections;
using Characters;
using TMPro;
using UnityEngine;

public class AddNumberLoop : MonoBehaviour
{
    private enum ShowVariable
    {
        Score,
        Kill,
        Life
    }

    [SerializeField] private ShowVariable variable;
    [SerializeField] private TextMeshProUGUI playerScore; // กำหนด TextMeshProUGUI ใน Inspector
    [SerializeField] private TextMeshProUGUI mapScore; // กำหนด TextMeshProUGUI ใน Inspector

    private int _numberVariable;
    private int _currentNumber;
    
    

    void Start()
    {
        if (playerScore == null)
        {
            playerScore = GetComponent<TextMeshProUGUI>(); // ค้นหา TextMeshProUGUI อัตโนมัติ
        }

        switch (variable)
        {
            case ShowVariable.Kill:
                _numberVariable = PlayerCharacter.Instance.Kill;
                break;
            case ShowVariable.Score:
                _numberVariable = (int)PlayerCharacter.Instance.Score;
                break;
            case ShowVariable.Life:
                _numberVariable = (int)PlayerCharacter.Instance.Life;
                break;
            default:
                return;
        }

        _currentNumber = 0; // เริ่มจาก 0
        StartCoroutine(AnimateScoreSmooth());
    }

    IEnumerator AnimateScoreSmooth()
    {
        float duration = 1f;
        float elapsed = 0f;
        int startScore = _currentNumber;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _currentNumber = (int)Mathf.Lerp(startScore, _numberVariable, elapsed / duration);
            playerScore.text = _currentNumber.ToString("N0"); // แสดงผลแบบมีเครื่องหมาย ,
            yield return null;
        }

        _currentNumber = _numberVariable;
        playerScore.text = _currentNumber.ToString("N0");
    }

    public void UpdateScore(int newScore)
    {
        StopAllCoroutines(); // หยุด Coroutine เก่าก่อน
        _numberVariable = newScore;
        StartCoroutine(AnimateScoreSmooth()); // เริ่มใหม่
    }

    public void SetToTargetNumber()
    {
        playerScore.text = _numberVariable.ToString("N0");
    }
    
}