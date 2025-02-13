using Characters;
using DG.Tweening;
using Skills;
using UnityEngine;
using UnityEngine.UI;

public class CooldownUI : MonoBehaviour
{
    [SerializeField] private Image mask;
    [SerializeField] private Image icon;
    private Image bg;
    [SerializeField] private Image flash;
    [SerializeField] private Sprite blackHole;
    [SerializeField] private Sprite perfectBubble;
    [SerializeField] private Sprite pathStriker;
    [SerializeField] private Material rgbMat;
    
    void Start()
    {
        bg = GetComponent<Image>();
        bg.color = Color.white;
        bg.material = rgbMat;
        NewSkill();
        PlayerCharacter.OnRandomNewSkill.AddListener(NewSkill);
        PlayerCharacter.Instance.SkillRight.onCooldownEnd.AddListener(PlayCooldown);
        PlayerCharacter.Instance.SkillRight.onSkillStart.AddListener(() =>
        {
            bg.material = null;
            bg.color = new Color(0f, 56f / 255f, 60f / 255f, 255f / 255f);
        });
    }

    private void NewSkill(SkillBase arg0)
    {
        throw new System.NotImplementedException();
    }

    // Update is called once per frame
    void Update()
    {
        float cooldownNormalize =
            Mathf.Clamp(PlayerCharacter.Instance.SkillRight.CooldownCounter / PlayerCharacter.Instance.SkillRight.Cooldown, 0, 1);
        mask.fillAmount = cooldownNormalize;
        if (cooldownNormalize == 0)
        {
            mask.color = Color.clear;
        }
        else
        {
            mask.color = new Color(0f, 0f, 0f, 172f / 255f);
        }
    }

    void PlayCooldown()
    {
        flash.DOFade(1, 0.5f).SetEase(Ease.InQuint).OnComplete(() => flash.DOFade(0, 0.3f).SetEase(Ease.OutQuint));
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
}
