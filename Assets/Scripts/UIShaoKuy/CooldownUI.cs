using System.Collections;
using System.Collections.Generic;
using Characters;
using DG.Tweening;
using Skills;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CooldownUI : MonoBehaviour
{
    private SkillBase _skill;
    private Image _image;
    [SerializeField] private MyEnum @enum;
    [SerializeField] private Image flash;

    enum MyEnum
    {
        left,
        right
    }
    void Start()
    {
        _image = GetComponent<Image>();
        if (@enum == MyEnum.left)
        {
            _skill = PlayerCharacter.Instance.SkillLeft;
        }
        else
        {
            _skill = PlayerCharacter.Instance.SkillRight;
        }

        _skill.onCooldownEnd.AddListener(PlayCooldown);
    }

    // Update is called once per frame
    void Update()
    {
        _image.fillAmount = 1 - Mathf.Clamp(_skill.CooldownCounter / _skill.Cooldown,0,1);
    }

    void PlayCooldown()
    {
        flash.DOFade(1, 0.5f).SetEase(Ease.InQuint).OnComplete(() => flash.DOFade(0, 0.3f).SetEase(Ease.OutQuint));
    }

    
}
