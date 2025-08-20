using System.Collections.Generic;
using Characters.SO.SkillDataSo;
using PixelUI;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.IngameModal
{
    public class SkillSlotModel : SerializedMonoBehaviour
    {
        [SerializeField] public Image skillIcon;
        [SerializeField] public ValueBar valueBar;
        [SerializeField] public TMP_Text cooldownText;
        [SerializeField] public Image skillframe;
        [SerializeField] public TMP_Text skillslotLv;
        [SerializeField] public GameObject skillLvBanner;

        public Dictionary<float, GameObject> OverloopVFX;
        const float epsilon = 0.0001f;

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

        public void OverloopFeedback(float multiply)
        {
            if (OverloopVFX == null) OverloopVFX = new Dictionary<float, GameObject>();
            foreach (var kvp in OverloopVFX)
                if (Mathf.Abs(kvp.Key - multiply) > epsilon && kvp.Value != null)
                    kvp.Value.SetActive(false);

            if (OverloopVFX.TryGetValue(multiply, out var vfxObj))
            {
                if (vfxObj != null) vfxObj.SetActive(true);
            }
            else
            {
                var newVFX = new GameObject($"OverloopVFX_{multiply}");
                newVFX.transform.SetParent(transform, false);
                newVFX.SetActive(true);

                OverloopVFX[multiply] = newVFX;
            }
        }
    }
}
