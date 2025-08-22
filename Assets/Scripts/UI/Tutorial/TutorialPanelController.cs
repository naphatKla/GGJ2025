using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameControl;
using GameControl.Controller;
using GameControl.GameState;
using Manager.SoundManager;
using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Tutorial
{
    public class TutorialPanelController : MonoBehaviour
    {
        [Header("Config")] public TutorialConfig tutorialConfig;

        [Header("UI Prefabs")] public GameObject pagePrefab, dotPrefab;

        [Header("UI References")] public Transform contentHolder, indicatorPanel;
        public Button leftButton, rightButton, skipButton;
        public CanvasGroup canvasGroup;

        [Header("Indicator Colors")] public Color activeColor = Color.cyan, inactiveColor = Color.gray;

        private readonly List<GameObject> pages = new();
        private readonly List<Image> indicators = new();
        private int currentIndex;
        private bool isAnimating;

        private void Start()
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

        private void GeneratePages()
        {
            foreach (var data in tutorialConfig.pages)
            {
                var page = Instantiate(pagePrefab, contentHolder);
                page.transform.localScale = Vector3.one;

                var titleText = page.transform.Find("TitleText")?.GetComponent<TMP_Text>();
                var descText = page.transform.Find("DescriptionText")?.GetComponent<TMP_Text>();
                var image = page.transform.Find("Image")?.GetComponent<Image>();

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

        private void GenerateIndicators()
        {
            foreach (var _ in pages)
            {
                var dot = Instantiate(dotPrefab, indicatorPanel);
                dot.transform.localScale = Vector3.one;
                indicators.Add(dot.GetComponent<Image>());
            }
        }

        private void ChangePage(int direction)
        {
            if (isAnimating) return;

            var newIndex = (currentIndex + direction + pages.Count) % pages.Count;
            StartCoroutine(AnimatePageChange(currentIndex, newIndex));
            currentIndex = newIndex;
        }

        private IEnumerator AnimatePageChange(int from, int to)
        {
            isAnimating = true;

            var fromPage = pages[from];
            var toPage = pages[to];

            fromPage.GetComponent<CanvasGroup>()
                .DOFade(0, 0.1f)
                .SetUpdate(true)
                .OnComplete(() => fromPage.SetActive(false));

            toPage.SetActive(true);
            var cg = toPage.GetComponent<CanvasGroup>();
            cg.alpha = 0;
            cg.DOFade(1, 0.1f).SetUpdate(true);

            UpdateIndicators(to);
            yield return new WaitForSecondsRealtime(0.01f);
            isAnimating = false;
        }

        private void ShowPage(int index, bool instant = false)
        {
            for (var i = 0; i < pages.Count; i++)
            {
                var isActive = i == index;
                pages[i].SetActive(isActive);
                if (pages[i].TryGetComponent(out CanvasGroup cg))
                    cg.alpha = isActive ? 1 : 0;
            }

            UpdateIndicators(index);
        }

        private void UpdateIndicators(int index)
        {
            for (var i = 0; i < indicators.Count; i++)
                indicators[i].color = i == index ? activeColor : inactiveColor;
        }

        private void CloseTutorial()
        {
            canvasGroup
                .DOFade(0, 0.5f)
                .SetUpdate(true)
                .OnComplete(() => gameObject.SetActive(false));

            PlayerPrefs.SetInt("HasSeenTutorial", 1);
            PlayerPrefs.Save();
            CountdownStart().Forget();
        }

        private async UniTaskVoid CountdownStart()
        {
            MMTimeScaleEvent.Trigger(MMTimeScaleMethods.Reset, 1, -1, false, 0f, false);
            SoundManager.Instance.PlayUI(SoundName.UI.CountDown5Sec);
            await GameTimer.Instance.StartCountdownAsync(5f);
            GameStateController.Instance.SetState(new StartState());
        }
    }
}