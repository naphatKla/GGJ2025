using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectorButton : MonoBehaviour
{
    [SerializeField] private Sprite toggle;
    [SerializeField] private Sprite normal;
    [SerializeField] private LevelSelector levelSelectorPar;
    [SerializeField] private GameObject NextButton;
    [SerializeField] private GameObject[] stars;
    private Image _image;
    void Start()
    {
        _image = GetComponent<Image>();
        levelSelectorPar = FindObjectOfType<LevelSelector>();
        StartCoroutine(StarCount());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnClick()
    {
        levelSelectorPar.SetLevel(this);
        NextButton.SetActive(true);
    }

    public void OnToggle()
    {
        _image.sprite = toggle;
        transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
    }

    public void TurnNormal()
    {
        _image.sprite = normal;
        transform.localScale = new Vector3(1f, 1f, 1f);
    }

    public IEnumerator StarCount()
    {
        LevelSelector levelSelector = FindObjectOfType<LevelSelector>();
        int levelIndex;
    
        if (int.TryParse(this.gameObject.name, out levelIndex)) // ป้องกัน error กรณีชื่อ object ไม่ใช่ตัวเลข
        {
            levelIndex--;
            if (levelIndex >= 0 && levelIndex < levelSelector.levelScores.Length) // ป้องกัน IndexOutOfRange
            {
                int score = levelSelector.levelScores[levelIndex];

                if (score > 0)
                {
                    for (int i = 0; i < Mathf.Min(score, stars.Length); i++) // ป้องกัน stars[i] เกินขอบเขต
                    {
                        stars[i].SetActive(true);
                        yield return new WaitForSeconds(1f);
                    }
                }
            }
            else
            {
                Debug.LogError($"Level index {levelIndex} is out of bounds!");
            }
        }
        else
        {
            Debug.LogError($"GameObject name '{this.gameObject.name}' is not a valid integer.");
        }
    }
}
