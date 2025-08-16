using PixelUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.IngameModal
{
    public class StatusSlotModel : MonoBehaviour
    {
        [SerializeField] public Image buffIcon;
        [SerializeField] public ValueBar valueBar;
        [SerializeField] public TMP_Text durationText;
        [SerializeField] public Image statusFrame;
    }
}
