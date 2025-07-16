using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TutorialConfig", menuName = "Tutorial/Tutorial Config")]
public class TutorialConfig : ScriptableObject
{
    public List<TutorialPageData> pages;
}