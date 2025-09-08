using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameControl.Controller;
using GameControl.GameState;
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
        private readonly List<TutorialPageView> pageViews = new();
        private readonly List<Image> indicators = new();
        private int currentIndex;
        private bool isAnimating;

        private void Start()
        {
            // PlayerPrefs.DeleteAll(); // ใช้เฉพาะตอนทดสอบ
            if (PlayerPrefs.GetInt("HasSeenTutorial", 0) == 1)
            {
                gameObject.SetActive(false);
                return;
            }

            GeneratePages();
            GenerateIndicators();

            leftButton.onClick.AddListener(() => ChangePage(-1));
            rightButton.onClick.AddListener(() => ChangePage(1));
            skipButton.onClick.AddListener(() => CloseTutorial().Forget());

            ShowPage(currentIndex, true);
        }

        private void GeneratePages()
        {
            foreach (var data in tutorialConfig.pages)
            {
                var page = Instantiate(pagePrefab, contentHolder);
                page.SetActive(false);
                page.transform.localScale = Vector3.one;

                var view = page.GetComponent<TutorialPageView>() ?? page.AddComponent<TutorialPageView>();
                view.Bind(data, tutorialConfig.globalTitleFont, tutorialConfig.globalDescriptionFont);

                if (!page.TryGetComponent(out CanvasGroup cg))
                    cg = page.AddComponent<CanvasGroup>();
                cg.alpha = 0;

                pages.Add(page);
                pageViews.Add(view);
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
            if (isAnimating || pages.Count == 0) return;
            var newIndex = (currentIndex + direction + pages.Count) % pages.Count;
            StartCoroutine(AnimatePageChange(currentIndex, newIndex));
            currentIndex = newIndex;
        }

        private IEnumerator AnimatePageChange(int from, int to)
        {
            if (from == to) yield break;
            isAnimating = true;

            var fromPage = pages[from];
            var toPage = pages[to];
            var fromView = pageViews[from];
            var toView = pageViews[to];

            fromView.Hide();
            fromPage.GetComponent<CanvasGroup>()
                .DOFade(0, 0.1f).SetUpdate(true)
                .OnComplete(() => fromPage.SetActive(false));

            toPage.SetActive(true);
            var cg = toPage.GetComponent<CanvasGroup>();
            cg.alpha = 0;
            cg.DOFade(1, 0.1f).SetUpdate(true);
            toView.Show();

            UpdateIndicators(to);
            yield return new WaitForSecondsRealtime(0.01f);
            isAnimating = false;
        }

        private void ShowPage(int index, bool instant = false)
        {
            for (int i = 0; i < pages.Count; i++)
            {
                bool active = i == index;
                pages[i].SetActive(active);
                if (pages[i].TryGetComponent(out CanvasGroup cg))
                    cg.alpha = active ? 1 : 0;

                if (active) pageViews[i].Show();
                else        pageViews[i].Hide();
            }

            UpdateIndicators(index);
        }

        private void UpdateIndicators(int index)
        {
            for (int i = 0; i < indicators.Count; i++)
                indicators[i].color = i == index ? activeColor : inactiveColor;
        }

        private async UniTask CloseTutorial()
        {
            try
            {
                PlayerPrefs.SetInt("HasSeenTutorial", 1);
                PlayerPrefs.Save();

                await UIManager.Instance.CloseSpecificPanel(UIPanelType.TutorialPanel);
                GameStateController.Instance.SetState(new PrestartState());
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Tutorial Cancel");
            }
        }
    }
}
