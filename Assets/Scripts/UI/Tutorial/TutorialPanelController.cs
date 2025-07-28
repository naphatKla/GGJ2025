using System.Collections;
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
    public GameObject pagePrefab, dotPrefab;

    [Header("UI References")]
    public Transform contentHolder, indicatorPanel;
    public Button leftButton, rightButton, skipButton;
    public CanvasGroup canvasGroup;

    [Header("Indicator Colors")]
    public Color activeColor = Color.cyan, inactiveColor = Color.gray;

    private List<GameObject> pages = new();
    private List<Image> indicators = new();
    private int currentIndex = 0;
    private bool isAnimating = false;

    void Start()
    { 
        PlayerPrefs.DeleteAll();
        
        if (PlayerPrefs.GetInt("HasSeenTutorial", 0) == 1)
        {
            gameObject.SetActive(false);
            return;
        }

        GeneratePages();
        GenerateIndicators();

        leftButton.onClick.AddListener(() => ChangePage(-1));
        rightButton.onClick.AddListener(() => ChangePage(1));
        skipButton.onClick.AddListener(CloseTutorial);

        ShowPage(currentIndex, true);
    }

    void GeneratePages()
    {
        foreach (var data in tutorialConfig.pages)
        {
            var page = Instantiate(pagePrefab, contentHolder);
            page.transform.localScale = Vector3.one;

            var titleText = page.transform.Find("TitleText")?.GetComponent<TMP_Text>();
            var descText  = page.transform.Find("DescriptionText")?.GetComponent<TMP_Text>();
            var image     = page.transform.Find("Image")?.GetComponent<Image>();

            if (titleText)
            {
                titleText.text = data.title;
                if (tutorialConfig.globalTitleFont) titleText.font = tutorialConfig.globalTitleFont;
            }

            if (descText)
            {
                descText.text = data.description;
                if (tutorialConfig.globalDescriptionFont) descText.font = tutorialConfig.globalDescriptionFont;
            }

            if (image)
            {
                image.sprite = data.image;
                image.gameObject.SetActive(data.image != null);
            }

            if (!page.TryGetComponent(out CanvasGroup cg))
                cg = page.AddComponent<CanvasGroup>();
            cg.alpha = 0;

            page.SetActive(false);
            pages.Add(page);
        }
    }

    void GenerateIndicators()
    {
        foreach (var _ in pages)
        {
            var dot = Instantiate(dotPrefab, indicatorPanel);
            dot.transform.localScale = Vector3.one;
            indicators.Add(dot.GetComponent<Image>());
        }
    }

    void ChangePage(int direction)
    {
        if (isAnimating) return;

        int newIndex = (currentIndex + direction + pages.Count) % pages.Count;
        StartCoroutine(AnimatePageChange(currentIndex, newIndex));
        currentIndex = newIndex;
    }

    IEnumerator AnimatePageChange(int from, int to)
    {
        isAnimating = true;

        var fromPage = pages[from];
        var toPage = pages[to];

        fromPage.GetComponent<CanvasGroup>().DOFade(0, 0.1f).OnComplete(() => fromPage.SetActive(false));
        toPage.SetActive(true);
        var cg = toPage.GetComponent<CanvasGroup>();
        cg.alpha = 0;
        cg.DOFade(1, 0.1f);

        UpdateIndicators(to);
        yield return new WaitForSeconds(0.01f);
        isAnimating = false;
    }

    void ShowPage(int index, bool instant = false)
    {
        for (int i = 0; i < pages.Count; i++)
        {
            bool isActive = i == index;
            pages[i].SetActive(isActive);
            if (pages[i].TryGetComponent(out CanvasGroup cg))
                cg.alpha = isActive ? 1 : 0;
        }
        UpdateIndicators(index);
    }

    void UpdateIndicators(int index)
    {
        for (int i = 0; i < indicators.Count; i++)
            indicators[i].color = (i == index) ? activeColor : inactiveColor;
    }

    void CloseTutorial()
    {
        canvasGroup.DOFade(0, 0.5f).OnComplete(() => gameObject.SetActive(false));
        PlayerPrefs.SetInt("HasSeenTutorial", 1);
        PlayerPrefs.Save();
    }
}
