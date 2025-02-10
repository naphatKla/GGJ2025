using System.Collections;
using System.Collections.Generic;
using Skills;
using UnityEngine;
using UnityEngine.UI;

public class CooldownUI : MonoBehaviour
{
    [SerializeField] private SkillBase skill;
    private Image _image;
    
    
    void Start()
    {
        _image = GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        _image.fillAmount = 1 - (skill.CooldownCounter / skill.Cooldown);
    }
}
