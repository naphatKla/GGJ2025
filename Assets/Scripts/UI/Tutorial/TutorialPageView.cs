using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Tutorial
{
    public class TutorialPageView : MonoBehaviour
    {
        [Header("UI Refs")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Image image;

        private TutorialPageData data;
        private TMP_FontAsset titleFont, bodyFont;
        private CancellationTokenSource gifCts;
        private bool isShowing;

        public void Bind(TutorialPageData pageData, TMP_FontAsset globalTitleFont, TMP_FontAsset globalBodyFont)
        {
            data = pageData;
            titleFont = globalTitleFont;
            bodyFont  = globalBodyFont;

            if (titleText)
            {
                titleText.text = data.title;
                if (titleFont) titleText.font = titleFont;
            }

            if (descriptionText)
            {
                descriptionText.text = data.description;
                if (bodyFont) descriptionText.font = bodyFont;
            }

            if (!image) return;

            if (data.mediaType == TutorialPageData.MediaType.Image)
            {
                image.sprite = data.image;
                image.enabled = data.image != null;
            }
            else
            {
                var first = (data.gif.frames != null && data.gif.frames.Length > 0) ? data.gif.frames[0] : null;
                image.sprite = first;
                image.enabled = first != null;
            }
        }

        public void Show()
        {
            isShowing = true;
            if (data != null && data.mediaType == TutorialPageData.MediaType.Gif)
                StartGif();
        }

        public void Hide()
        {
            isShowing = false;
            StopGif();

            if (data != null && data.mediaType == TutorialPageData.MediaType.Gif && image && data.gif.holdFirstFrameOnStop)
            {
                var frames = data.gif.frames;
                if (frames != null && frames.Length > 0) image.sprite = frames[0];
            }
        }

        private void StartGif()
        {
            StopGif();

            var frames = data.gif.frames;
            if (image == null || frames == null || frames.Length == 0) return;

            gifCts = new CancellationTokenSource();
            var token = gifCts.Token;
            float fps = Mathf.Max(0.1f, data.gif.fps);

            UniTask.Void(async () =>
            {
                do
                {
                    for (int i = 0; i < frames.Length; i++)
                    {
                        if (token.IsCancellationRequested || !isShowing) return;
                        image.sprite = frames[i];
                        await UniTask.Delay(System.TimeSpan.FromSeconds(1f / fps), ignoreTimeScale: true, cancellationToken: token);
                    }
                } while (data.gif.loop && !token.IsCancellationRequested && isShowing);
            });
        }

        private void StopGif()
        {
            if (gifCts == null) return;
            gifCts.Cancel();
            gifCts.Dispose();
            gifCts = null;
        }

        private void OnDisable() => Hide();

#if UNITY_EDITOR
        private void Reset()
        {
            if (!titleText) titleText = transform.Find("TitleText")?.GetComponent<TMP_Text>();
            if (!descriptionText) descriptionText = transform.Find("DescriptionText")?.GetComponent<TMP_Text>();
            if (!image) image = transform.Find("Image")?.GetComponent<Image>();
        }
#endif
    }
}
