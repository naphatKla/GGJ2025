using Challenge;
using Coffee.UIEffects;
using Demo;
using Player;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace UI.Milestone
{
    public class MilestoneViewholder : ScrollIndexCallbackBase
    {
        [Title("Viewholder")] 
        public TMP_Text levelVh;
        public Image imageVh;
        
        [Title("Display")] 
        public UIEffect uiEffect;
        public Gradient selectGradient;
        
        [Title("Unlock Display")] 
        public Color unlockColor;
        
        [Title("Lock Display")] 
        public Color lockColor;
        
        public void UpdateViewholder(int index, ChallengeDataSO content, MapStat mapStat)
        {
            if (mapStat.MaxLevelUnlockMilestone >= index) //Unlock
            {
                if (imageVh != null) imageVh.color = unlockColor;
                if (levelVh != null) levelVh.text = "LV."+index;
            }
            else
            {
                if (imageVh != null) imageVh.color = lockColor;
                if (levelVh != null) levelVh.text = "LOCKED";
            }
        }
        
        public void SetClickedColor(bool isClicked)
        {
            if (m_Button == null || m_Button.image == null) return;
            if (isClicked)
            {
                uiEffect.shadowMode = ShadowMode.Outline;
                ApplyGradient(uiEffect, selectGradient);
            }
            else
            {
                uiEffect.shadowMode = ShadowMode.None;
                uiEffect.gradationMode = GradationMode.None;
            }
        }
        
        private void ApplyGradient(UIEffect effect, Gradient gradient)
        {
            if (!effect || gradient == null) return;
            effect.gradationMode = GradationMode.AngleGradient;
            effect.gradationIntensity = 1f;
            effect.SetGradientKeys(
                gradient.colorKeys,
                gradient.alphaKeys,
                gradient.mode
            );
        }
    }
}
