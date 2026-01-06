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
        
        public void UpdateViewholder()
        {

            
        }
        
        public void SetClickedColor(bool isClicked)
        {
            if (m_Button == null || m_Button.image == null) return;
            if (isClicked)
            {
                uiEffect.shadowMode = ShadowMode.Outline;
                uiEffect.gradationMode = GradationMode.AngleGradient;
            }
            else
            {
                uiEffect.edgeMode = EdgeMode.None;
                uiEffect.gradationMode = GradationMode.None;
            }
        }
    }
}
