using UnityEngine;

[CreateAssetMenu(fileName = "TutorialPageData", menuName = "Tutorial/Tutorial Page Data")]
public class TutorialPageData : ScriptableObject
{
    public string title;
    [TextArea(3,10)]
    public string description;
    public Sprite image;
}