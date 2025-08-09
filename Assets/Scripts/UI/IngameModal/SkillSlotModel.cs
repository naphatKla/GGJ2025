using PixelUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.IngameModal
{
    public class SkillSlotModel : MonoBehaviour
    {
        [SerializeField] public Image skillIcon;
        [SerializeField] public ValueBar valueBar;
        [SerializeField] public TMP_Text cooldownText;
    }
}
