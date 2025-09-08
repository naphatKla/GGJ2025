using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "TutorialPageData", menuName = "Tutorial/Tutorial Page Data")]
public class TutorialPageData : ScriptableObject
{
    [Title("Text")]
    public string title;
    [TextArea(3,10)]
    public string description;

    public enum MediaType { Image, Gif }

    [Title("Media")]
    [EnumToggleButtons]
    public MediaType mediaType = MediaType.Image;

    [ShowIf("@mediaType == MediaType.Image")]
    [PreviewField(90, ObjectFieldAlignment.Left)]
    public Sprite image;

    [System.Serializable]
    public class GifClip
    {
        [Tooltip("ใส่เฟรมจากการ Slice GIF/สไปรต์ชีต")]
        public Sprite[] frames;
        [Min(0.1f)] public float fps = 12f;
        public bool loop = true;
        public bool holdFirstFrameOnStop = true;
    }

    [ShowIf("@mediaType == MediaType.Gif")]
    public GifClip gif = new GifClip();
}