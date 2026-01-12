using Coffee.UIEffects;
using Demo;
using DotNotify;
using GameControl.SO;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MapSelectionRework
{
    public class MapSelectionIndexViewholder : ScrollIndexCallbackBase
    {
        [Title("Viewholder")] 
        public TMP_Text nameVh;
        public TMP_Text descriptionVh;
        public Image imageVh;
        public UIEffect uieffect;
        public RedDotView reddotNoti;

        [Title("Lock Display")] 
        public bool IsLocked;
        public Sprite lockimageVh;

        public void UpdateViewholder(MapDataSO mapData)
        {
            UpdateDotNotify(mapData.mapId);
            if (!IsLocked)
            {
                if (nameVh != null) nameVh.text = mapData.mapName.ToUpper();
                if (descriptionVh != null) descriptionVh.text = mapData.description;
                if (imageVh != null) imageVh.sprite = mapData.image;
            }
            else
            {
                if (nameVh != null) nameVh.text = "LOCKED";
                if (descriptionVh != null) descriptionVh.text = mapData.lockdescription;
                if (imageVh != null) imageVh.sprite = lockimageVh;
            }
        }
        
        public void UpdateDotNotify(string id)
        {
            reddotNoti.KeyDot = "Map:" + id;
        }
        
        public void SetClickedColor(bool isClicked)
        {
            if (m_Button == null || m_Button.image == null) return;
            if (isClicked)
            {
                uieffect.edgeMode = EdgeMode.Shiny;
                uieffect.gradationMode = GradationMode.HorizontalGradient;
            }
            else
            {
                uieffect.edgeMode = EdgeMode.None;
                uieffect.gradationMode = GradationMode.None;
            }
        }
    }
}
