using Achievements;
using Coffee.UIEffects;
using Demo;
using PixelUI;
using Player;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Achievement
{
    public class AchievementViewholder : ScrollIndexCallbackBase
    {
        [Title("Viewholder")] 
        public TMP_Text nameVh;
        public TMP_Text descriptionVh;
        public Image imageVh;
        public ValueBar progressionBar;
        public TMP_Text progressionValue;
        public TMP_Text progressionPercent;

        [Title("Display")] 
        public bool IsLocked;
        public Sprite lockimageVh;
        public UIEffect uiEffect;
        public Color unlockColorBg;
        public Gradient lockGradient;
        public Color lockColorBg;
        public Gradient unlockGradient;

        public void UpdateViewholder(AchievementEntry achievementData, PlayerData playerData)
        {
            if (!IsLocked)
            {
                if (imageVh != null) imageVh.sprite = achievementData.icon;
                if (nameVh != null) nameVh.text = achievementData.displayName;
                if (uiEffect != null) ApplyUnlockGradient();
                if (descriptionVh != null) descriptionVh.text = achievementData.description;
                m_Button.interactable = false;
                m_Button.image.color = unlockColorBg;
                UpdateProgressionBar(achievementData, playerData);
            }
            else
            {
                if (imageVh != null) imageVh.sprite = lockimageVh;
                if (nameVh != null) nameVh.text = achievementData.displayName;
                if (uiEffect != null) ApplyLockGradient();
                if (descriptionVh != null) descriptionVh.text = achievementData.description;
                m_Button.interactable = false;
                m_Button.image.color = lockColorBg;
                UpdateProgressionBar(achievementData, playerData);
            }
        }
        
        public void UpdateProgressionBar(AchievementEntry achievementData, PlayerData playerData)
        {
            if (!achievementData.TryGetMainProgress(playerData, out int current, out int target))
            {
                progressionBar.gameObject.SetActive(false);
                progressionValue.gameObject.SetActive(false);
                progressionPercent.gameObject.SetActive(false);
                return;
            }
            
            if (target <= 0)
            {
                progressionBar.gameObject.SetActive(false);
                progressionValue.gameObject.SetActive(false);
                progressionPercent.gameObject.SetActive(false);
                return;
            }

            // ถ้ามีข้อมูลค่อยโชว์
            progressionBar.gameObject.SetActive(true);
            progressionValue.gameObject.SetActive(true);
            progressionPercent.gameObject.SetActive(true);

            // กัน current โผล่เกิน target
            current = Mathf.Clamp(current, 0, target);

            progressionBar.MinValue = 0;
            progressionBar.MaxValue = target;
            progressionBar.CurrentValue = current;

            progressionValue.text = $"{current}/{target}";

            float percentProgress = (float)current / target * 100f;
            progressionPercent.text = $"{percentProgress:F2} %";
        }

        
        public void ApplyLockGradient()
        {
            ApplyGradient(uiEffect, lockGradient);
        }

        public void ApplyUnlockGradient()
        {
            ApplyGradient(uiEffect, unlockGradient);
        }
        
        private void ApplyGradient(UIEffect effect, Gradient gradient)
        {
            if (!effect || gradient == null) return;
            effect.gradationMode = GradationMode.HorizontalGradient;
            effect.gradationIntensity = 1f;
            effect.SetGradientKeys(
                gradient.colorKeys,
                gradient.alphaKeys,
                gradient.mode
            );
        }
    }
}
