using Characters;
using DG.Tweening;
using Skills;
using UnityEngine;
using UnityEngine.UI;

public class CooldownUI : MonoBehaviour
{
    #region Inspectors & Fields
    [Header("UI Elements")]
    [SerializeField] private Image mask;
    [SerializeField] private Image icon;
    [SerializeField] private Image flash;
    [SerializeField] private GameObject mouse;
    [Header("SkillsSprite")]
    [SerializeField] private Sprite blackHole;
    [SerializeField] private Sprite perfectBubble;
    [SerializeField] private Sprite pathStriker;
    [Header("Materials")]
    [SerializeField] private Material rgbMat;
    #endregion -------------------------------------------------------------------------------------------------------------------
    
    #region Properties
    private Image bg;
    #endregion -------------------------------------------------------------------------------------------------------------------
    
    #region UnityMethods
    void Start()
    {
        bg = GetComponent<Image>();
        bg.color = Color.white;
        bg.material = rgbMat;
        NewSkill();
        PlayerCharacter.OnRandomNewSkill.AddListener(NewSkill);
        foreach (var skill in PlayerCharacter.Instance.SkillDictionary)
        {
            skill.Value.onCooldownEnd.AddListener(() =>
            {
                if (PlayerCharacter.Instance.SkillRight == skill.Value)
                    PlayCooldown();
            });
            skill.Value.onSkillStart.AddListener(() =>
            {
                if (PlayerCharacter.Instance.SkillRight == skill.Value)
                {
                    bg.material = null;
                    bg.color = new Color(0f, 56f / 255f, 60f / 255f, 255f / 255f);
                }
            });
        }
    }
    
    void Update()
    {
        float cooldownNormalize =
            Mathf.Clamp(PlayerCharacter.Instance.SkillRight.CooldownCounter / PlayerCharacter.Instance.SkillRight.Cooldown, 0, 1);
        mask.fillAmount = cooldownNormalize;
        if (cooldownNormalize == 0)
        {
            mask.color = Color.clear;
            mouse.SetActive(true);
        }
        else
        {
            mask.color = new Color(0f, 0f, 0f, 172f / 255f);
            mouse.SetActive(false);
        }
    }
    #endregion -------------------------------------------------------------------------------------------------------------------
    #region Methods
    private void NewSkill(SkillBase arg0)
    {
        throw new System.NotImplementedException();
    }

    void PlayCooldown()
    {
        flash.DOFade(1, 0.5f).SetEase(Ease.InQuint).OnComplete(() => flash.DOFade(0, 0.3f).SetEase(Ease.OutQuint));
        mouse.SetActive(true);
        bg.color = Color.white;
        bg.material = rgbMat;
    }

    void NewSkill()
    {
        if (PlayerCharacter.Instance.SkillRight is SkillBlackHole)
        {
            icon.sprite = blackHole;
        }
        else if (PlayerCharacter.Instance.SkillRight is SkillPerfectBubble)
        {
            icon.sprite = perfectBubble;
        }
        else if (PlayerCharacter.Instance.SkillRight is SkillPathStriker)
        {
            icon.sprite = pathStriker;
        }
        else
        {
            return;   
        }

        PlayCooldown();
    }
    #endregion -------------------------------------------------------------------------------------------------------------------

    
    // Update is called once per frame
    

    
}
