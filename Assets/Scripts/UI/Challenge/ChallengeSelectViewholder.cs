using System.Collections;
using System.Collections.Generic;
using Challenge;
using Coffee.UIEffects;
using Demo;
using DotNotify;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Challenge
{
    public class ChallengeSelectViewholder : ScrollIndexCallbackBase
    {
        [Title("Viewholder")] 
        public TMP_Text nameVh;
        public TMP_Text descriptionVh;
        public RedDotView reddotNoti;

        [Title("Display")] 
        public UIEffect selectedUIEffect;
        public UIEffect uiEffect;
        public Color unlockColorBg;
        public Gradient lockGradient;
        public Color lockColorBg;
        public Gradient unlockGradient;
        
        public bool IsLocked;
        
        public void UpdateViewholder(ChallengeDataSO challengeData)
        {
            UpdateDotNotify(challengeData.id);
            if (!IsLocked)
            {
                if (nameVh != null) nameVh.text = challengeData.title;
                if (uiEffect != null) ApplyUnlockGradient();
                if (descriptionVh != null) descriptionVh.text = challengeData.description;
                m_Button.interactable = true;
                m_Button.image.color = unlockColorBg;
            }
            else
            {
                if (nameVh != null) nameVh.text = "LOCKED";
                if (uiEffect != null) ApplyLockGradient();
                if (descriptionVh != null) descriptionVh.text = challengeData.lockdescription;
                m_Button.interactable = false;
                m_Button.image.color = lockColorBg;
            }
        }
        
        public void UpdateDotNotify(string id)
        {
            reddotNoti.KeyDot = "Challenge:" + id;
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
            if (isClicked)
            {
                selectedUIEffect.transitionFilter = TransitionFilter.Pattern;
                selectedUIEffect.edgeMode = EdgeMode.Shiny;
                selectedUIEffect.gradationMode = GradationMode.HorizontalGradient;
            }
            else
            {
                selectedUIEffect.transitionFilter = TransitionFilter.None;
                selectedUIEffect.edgeMode = EdgeMode.None;
                selectedUIEffect.gradationMode = GradationMode.None;
            }
        }
    }

}

