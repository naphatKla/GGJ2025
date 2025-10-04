using System.Collections.Generic;
using Characters.SO.SkillDataSo;
using Manager.SoundManager;
using PixelUI;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.IngameViewholder
{
    public class SkillSlotViewholder : SerializedMonoBehaviour
    {
        [SerializeField] public Image skillIcon;
        [SerializeField] public ValueBar valueBar;
        [SerializeField] public TMP_Text cooldownText;
        [SerializeField] public Image skillframe;
        [SerializeField] public TMP_Text skillslotLv;
        [SerializeField] public GameObject skillLvBanner;
        [SerializeField] public ParticleSystem cooldownFinishParticle;
        
        [SerializeField] private Dictionary<float, GameObject> VFXPrefabs;
        private Dictionary<float, GameObject> OverloopVFX;

        const float epsilon = 0.0001f;

        public void UpdateLevelText(float level, BaseSkillDataSo skill)
        {
            skillLvBanner.SetActive(true);
            skillslotLv.text = $"LV.<color=#FFD700>{level:F0}</color>";
            cooldownText.text = "";
            skillIcon.sprite = skill.SkillIcon;
        }
        
        public void PlayCooldownFinishFeedback()
        {
            cooldownFinishParticle.Play();
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
                if (VFXPrefabs.TryGetValue(multiply, out var prefab))
                {
                    var newVFX = Instantiate(prefab, transform);
                    newVFX.name = $"OverloopVFX_{multiply}";
                    newVFX.SetActive(true);

                    OverloopVFX[multiply] = newVFX;
                }
            }
        }
    }
}
