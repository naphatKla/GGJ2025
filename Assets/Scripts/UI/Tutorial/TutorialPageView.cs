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

        [Header("Optional Override")]
        [Tooltip("ตั้งค่าไว้ถ้าต้องการ override Sprite Asset ต่อหน้า (ถ้าไม่ตั้ง จะใช้จาก Config)")]
        [SerializeField] private TMP_SpriteAsset spriteAssetOverride;

        private TutorialPageData data;
        private TMP_FontAsset titleFont, bodyFont;
        private TMP_SpriteAsset spriteAsset;
        private CancellationTokenSource gifCts;
        private bool isShowing;

        public void Bind(
            TutorialPageData pageData,
            TMP_FontAsset globalTitleFont,
            TMP_FontAsset globalBodyFont,
            TMP_SpriteAsset globalSpriteAsset = null)
        {
            data        = pageData;
            titleFont   = globalTitleFont;
            bodyFont    = globalBodyFont;
            spriteAsset = spriteAssetOverride ? spriteAssetOverride : globalSpriteAsset;

            if (titleText)
            {
                titleText.text = data.title;
                if (titleFont) titleText.font = titleFont;
            }

            if (descriptionText)
            {
                // สำคัญ: ตั้ง Sprite Asset เพื่อให้ <sprite name="..."> แสดงผล
                if (spriteAsset) descriptionText.spriteAsset = spriteAsset;

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
