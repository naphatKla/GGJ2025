using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class TutorialPanelController : MonoBehaviour
{
    [Header("Config")]
    public TutorialConfig tutorialConfig;

    [Header("UI Prefabs")]
    public GameObject pagePrefab;
    public GameObject dotPrefab;

    [Header("UI References")]
    public Transform contentHolder;
    public Transform indicatorPanel;
    public Button leftButton;
    public Button rightButton;
    public Button skipButton;
    public CanvasGroup canvasGroup;

    [Header("Indicator Colors")]
    public Color activeColor = Color.cyan;
    public Color inactiveColor = Color.gray;

    private List<GameObject> pages = new List<GameObject>();
    private List<Image> indicators = new List<Image>();

    private int currentIndex = 0;
    private bool isAnimating = false;

    private void Start()
    {
        // ตรวจว่าผู้เล่นเคยดู Tutorial แล้วหรือยัง
        if (PlayerPrefs.GetInt("HasSeenTutorial", 0) == 1)
        {
            this.gameObject.SetActive(false);
            return;
        }

        GeneratePages();
        GenerateIndicators();

        leftButton.onClick.AddListener(() => ChangePage(-1));
        rightButton.onClick.AddListener(() => ChangePage(1));
        skipButton.onClick.AddListener(CloseTutorial);

        ShowPage(currentIndex, instant:true);
    }

    void GeneratePages()
    {
        foreach (var data in tutorialConfig.pages)
        {
            GameObject page = Instantiate(pagePrefab, contentHolder);
            page.transform.localScale = Vector3.one;

            var titleText = page.transform.Find("TitleText").GetComponent<TMP_Text>();
            var descText = page.transform.Find("DescriptionText").GetComponent<TMP_Text>();
            var image = page.transform.Find("Image").GetComponent<Image>();

            titleText.text = data.title;
            descText.text = data.description;

            if (data.image != null)
            {
                image.sprite = data.image;
                image.gameObject.SetActive(true);
            }
            else
            {
                image?.gameObject.SetActive(false);
            }

            page.SetActive(false);
            pages.Add(page);
        }
    }

    void GenerateIndicators()
    {
        for (int i = 0; i < pages.Count; i++)
        {
            var dot = Instantiate(dotPrefab, indicatorPanel);
            dot.transform.localScale = Vector3.one;
            indicators.Add(dot.GetComponent<Image>());
        }
    }

    void ChangePage(int direction)
    {
        if (isAnimating) return;

        int newIndex = currentIndex + direction;
        if (newIndex < 0) newIndex = pages.Count - 1;
        if (newIndex >= pages.Count) newIndex = 0;

        StartCoroutine(AnimatePageChange(currentIndex, newIndex));
        currentIndex = newIndex;
    }

    System.Collections.IEnumerator AnimatePageChange(int fromIndex, int toIndex)
    {
        isAnimating = true;

        pages[fromIndex].GetComponent<CanvasGroup>().DOFade(0, 0.3f)
            .OnComplete(() => pages[fromIndex].SetActive(false));

        pages[toIndex].SetActive(true);
        var cg = pages[toIndex].GetComponent<CanvasGroup>();
        cg.alpha = 0;
        cg.DOFade(1, 0.3f);

        UpdateIndicators(toIndex);

        yield return new WaitForSeconds(0.3f);
        isAnimating = false;
    }

    void ShowPage(int index, bool instant = false)
    {
        for (int i = 0; i < pages.Count; i++)
        {
            pages[i].SetActive(i == index);
            if (pages[i].TryGetComponent<CanvasGroup>(out var cg))
                cg.alpha = (i == index) ? 1 : 0;
        }

        UpdateIndicators(index);
    }

    void UpdateIndicators(int index)
    {
        for (int i = 0; i < indicators.Count; i++)
        {
            indicators[i].color = (i == index) ? activeColor : inactiveColor;
        }
    }

    void CloseTutorial()
    {
        canvasGroup.DOFade(0, 0.5f).OnComplete(() =>
        {
            gameObject.SetActive(false);
        });

        PlayerPrefs.SetInt("HasSeenTutorial", 1);
        PlayerPrefs.Save();
    }
}
