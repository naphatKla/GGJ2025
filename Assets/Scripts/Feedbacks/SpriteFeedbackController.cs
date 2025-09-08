using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class SpriteFeedbackController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private GameObject bodyObj;
    [SerializeField] private SpriteRenderer flashBody;
    
    [SerializeField] [FoldoutGroup("Shake")] private float shakeDuration = 0.25f;
    [SerializeField] [FoldoutGroup("Shake")] private Vector3 shakeStrength = new Vector3(1,1,0);
    [SerializeField] [FoldoutGroup("Shake")] private int shakeVibrato = 1;
    [SerializeField] [FoldoutGroup("Shake")] private float shakeRandomness = 90;
    [SerializeField] [FoldoutGroup("Shake")] private bool shakeSnapping = false;
    [SerializeField] [FoldoutGroup("Shake")] private bool shakeFadeOut = true;
    [SerializeField] [FoldoutGroup("Shake")] private ShakeRandomnessMode randomnessMode = ShakeRandomnessMode.Harmonic;

    [SerializeField] [FoldoutGroup("Sprite")] private Color baseColor = Color.clear;
    [SerializeField] [FoldoutGroup("Sprite")] [ColorPalette] private Color hitColor = Color.white;
    [SerializeField] [FoldoutGroup("Sprite")] float toWhiteDuration = 0.15f;
    [SerializeField] [FoldoutGroup("Sprite")] float toBaseDuration = 0.15f;

    private Tween shakeSpriteTween;
    private Tween colorChangeSpriteTween;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShakeSprite()
    {
        if (shakeSpriteTween.IsActive()) return;
        shakeSpriteTween = bodyObj.transform.DOShakePosition(shakeDuration, shakeStrength, 1,
            shakeRandomness,shakeSnapping,shakeFadeOut,ShakeRandomnessMode.Harmonic);
    }

    public void ChangeSpriteColor()
    {
        if (colorChangeSpriteTween.IsActive()) return;
        colorChangeSpriteTween = flashBody.DOColor(hitColor,toWhiteDuration).OnComplete(() => flashBody.DOColor(baseColor,toBaseDuration));
    }
}
