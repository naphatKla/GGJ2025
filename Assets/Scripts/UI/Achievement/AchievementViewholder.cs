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
            achievementData.TryGetMainProgress(playerData, out int current, out int target);

            if (current == 0 && target == 0)
            {
                progressionBar.gameObject.SetActive(false);
                progressionValue.gameObject.SetActive(false);
                progressionPercent.gameObject.SetActive(false);
                return;
            }
            else
            {
                progressionBar.gameObject.SetActive(true);
                progressionValue.gameObject.SetActive(true);
                progressionPercent.gameObject.SetActive(true);
            }
            
            progressionBar.MinValue = 0;
            progressionBar.MaxValue = target;
            progressionBar.CurrentValue = current;
            progressionValue.text = $"{current}/{target}";
            var percentProgress = (current / target) * 100;
            progressionPercent.text = percentProgress + "%";
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
        
        public void SetClickedColor(bool isClicked)
        {
            if (m_Button == null || m_Button.image == null) return;
            m_Button.image.color = isClicked ? Color.green : Color.white;
        }

    }
}
