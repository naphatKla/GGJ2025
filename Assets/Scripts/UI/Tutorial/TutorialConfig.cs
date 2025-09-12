using System.Collections.Generic;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "TutorialConfig", menuName = "Tutorial/Tutorial Config")]
public class TutorialConfig : ScriptableObject
{
    public List<TutorialPageData> pages;

    [Header("Global Fonts")]
    public TMP_FontAsset globalTitleFont;
    public TMP_FontAsset globalDescriptionFont;

    [Header("Global Sprite Asset")]
    [Tooltip("TMP Sprite Asset สำหรับไอคอนใน Description เช่น <sprite name=\"lmb\">")]
    public TMP_SpriteAsset globalSpriteAsset;
}