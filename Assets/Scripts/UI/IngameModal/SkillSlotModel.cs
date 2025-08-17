using Characters.SO.SkillDataSo;
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
        [SerializeField] public Image skillframe;
        [SerializeField] public TMP_Text skillslotLv;
        [SerializeField] public GameObject skillLvBanner;

        public void UpdateLevelText(float level, BaseSkillDataSo skill)
        {
            skillLvBanner.SetActive(true);
            skillslotLv.text = $"LV.<color=#FFD700>{level:F0}</color>";
            cooldownText.text = "";
            skillIcon.sprite = skill.SkillIcon;
        }
        
        public void ResetSkillSlot()
        {
            cooldownText.text = "";
            valueBar.CurrentValue = 0;
        }
    }
}
