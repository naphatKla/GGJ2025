using System.Collections;
using System.Collections.Generic;
using Characters.SO.SkillDataSo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.IngameModal
{
    public class SolfUpgradeModel : MonoBehaviour
    {
        [SerializeField] private TMP_Text skillTitle;
        [SerializeField] private Image skillIcon;
        [SerializeField] private TMP_Text skillDescription;
        [SerializeField] private Button selectButton;

        public Button SelectButton => selectButton;

        public void UpdateUIModal(BaseSkillDataSo data)
        {
            skillTitle.text = data.name;
            skillIcon.sprite = data.SkillIcon;
            skillDescription.text = data.SkillDescription;
        }
    }
}