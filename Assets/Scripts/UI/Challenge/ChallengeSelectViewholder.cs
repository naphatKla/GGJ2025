using System.Collections;
using System.Collections.Generic;
using Challenge;
using Demo;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace UI.Challenge
{
    public class ChallengeSelectViewholder : ScrollIndexCallbackBase
    {
        [Title("Viewholder")] 
        public TMP_Text nameVh;
        public TMP_Text descriptionVh;

        [Title("Lock Display")] 
        public bool IsLocked;
        public Sprite lockimageVh;
        
        public void UpdateViewholder(ChallengeDataSO challengeData)
        {
            if (!IsLocked)
            {
                if (nameVh != null) nameVh.text = challengeData.title;
                if (descriptionVh != null) descriptionVh.text = challengeData.description;
            }
            else
            {
                if (nameVh != null) nameVh.text = "LOCKED";
                if (descriptionVh != null) descriptionVh.text = challengeData.lockdescription;
            }
        }
        public void SetClickedColor(bool isClicked)
        {
            if (m_Button == null || m_Button.image == null) return;
            m_Button.image.color = isClicked ? Color.green : Color.white;
        }
    }

}

