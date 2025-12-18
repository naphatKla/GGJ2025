using System.Collections;
using System.Collections.Generic;
using Demo;
using DotNotify;
using GameControl.SO;
using PermanentUpgrade;
using PixelUI;
using Player;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.PermanentUpgrade
{
    public class PermanentUpgradeViewholder : ScrollIndexCallbackBase
    {
        [Title("Viewholder")] 
        public TMP_Text nameVh;
        public TMP_Text levelVh;
        public Image imageVh;
        public GameObject selectFrame;

        public ValueBar valueBar;
        public RedDotView reddot;

        [Title("Lock Display")] 
        public bool IsLocked;
        public GameObject lockimageObj;

        public void UpdateViewholder(PermanentUpgradeEntry pEntry, PlayerData playerData)
        {
            if (lockimageObj != null) lockimageObj.SetActive(IsLocked);
            UpdateDotNotify(pEntry.type.ToString());
            if (nameVh != null) nameVh.text = pEntry.nameUpgrade;
            if (imageVh != null) imageVh.sprite = pEntry.iconUpgrade;
            var level = playerData.GetPermanentUpgradeLevel(pEntry.type);
            if (levelVh != null) levelVh.text = "LEVEL "+ level;
            if (valueBar != null)
            {
                valueBar.MinValue = 0;
                valueBar.MaxValue = pEntry.MaxLevel;
                valueBar.CurrentValue = level;
                valueBar.UpdateUI();
            }
        }
        
        public void UpdateDotNotify(string id)
        {
            reddot.KeyDot = "Permanent:" + id;
        }
        
        public void SetClickedColor(bool isClicked)
        {
            if (m_Button == null || m_Button.image == null) return;
            selectFrame.SetActive(isClicked);
        }
    }
}
