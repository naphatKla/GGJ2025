using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

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

        private Sprite[] sheetFrames;

        public void Bind(
            TutorialPageData pageData,
            TMP_FontAsset globalTitleFont,
            TMP_FontAsset globalBodyFont,
            TMP_SpriteAsset globalSpriteAsset = null)
        {
            data = pageData;
            titleFont = globalTitleFont;
            bodyFont = globalBodyFont;
            spriteAsset = spriteAssetOverride ? spriteAssetOverride : globalSpriteAsset;

            if (titleText)
            {
                titleText.text = data.title;
                if (titleFont) titleText.font = titleFont;
            }

            if (descriptionText)
            {
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
                PrepareSpriteSheet();
                var first = (sheetFrames != null && sheetFrames.Length > 0) ? sheetFrames[0] : null;
                image.sprite = first;
                image.enabled = first != null;
            }
        }

        public void Show()
        {
            isShowing = true;
            if (data != null && data.mediaType == TutorialPageData.MediaType.SpriteSheet)
                StartSpriteSheet();
        }

        public void Hide()
        {
            isShowing = false;
            StopGif();

            if (data != null && data.mediaType == TutorialPageData.MediaType.SpriteSheet
                && image && data.spriteSheetClip.holdFirstFrameOnStop)
            {
                if (sheetFrames != null && sheetFrames.Length > 0)
                    image.sprite = sheetFrames[0];
            }
        }

        private void PrepareSpriteSheet()
        {
            if (data.spriteSheetClip.spriteSheet == null) return;

#if UNITY_EDITOR
            // โหลด sub-sprites ทั้งหมดจาก sprite sheet
            string path = AssetDatabase.GetAssetPath(data.spriteSheetClip.spriteSheet);
            var objs = AssetDatabase.LoadAllAssetsAtPath(path);
            sheetFrames = objs.OfType<Sprite>().OrderBy(s => s.name, new NaturalComparer()).ToArray();
#else
            // runtime: Unity โหลด sub-sprites จาก Resources.LoadAll
            sheetFrames = Resources.LoadAll<Sprite>(data.spriteSheetClip.spriteSheet.name);
#endif
        }

        private void StartSpriteSheet()
        {
            StopGif();

            if (image == null || sheetFrames == null || sheetFrames.Length == 0) return;

            gifCts = new CancellationTokenSource();
            var token = gifCts.Token;
            float fps = Mathf.Max(0.1f, data.spriteSheetClip.fps);

            UniTask.Void(async () =>
            {
                do
                {
                    for (int i = 0; i < sheetFrames.Length; i++)
                    {
                        if (token.IsCancellationRequested || !isShowing) return;
                        image.sprite = sheetFrames[i];
                        await UniTask.Delay(System.TimeSpan.FromSeconds(1f / fps),
                            ignoreTimeScale: true,
                            cancellationToken: token);
                    }
                } while (data.spriteSheetClip.loop && !token.IsCancellationRequested && isShowing);
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

    /// <summary>
    /// ใช้สำหรับ sort sprite ที่ slice ออกมาให้เป็นลำดับตัวเลขถูกต้อง เช่น frame_1, frame_2...
    /// </summary>
    internal class NaturalComparer : System.Collections.Generic.IComparer<string>
    {
        public int Compare(string a, string b)
        {
            return EditorUtility.NaturalCompare(a, b);
        }
    }
}
