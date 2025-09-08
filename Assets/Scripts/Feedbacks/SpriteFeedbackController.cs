using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SpriteFeedbackController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private GameObject bodyObj;
    [SerializeField] private float shakeDuration = 0.25f;
    [SerializeField] private Vector3 shakeStrength = new Vector3(1,1,0);
    [SerializeField] private float shakeVibrato = 1f;
    [SerializeField] private float shakeRandomness = 90;
    [SerializeField] private bool shakeSnapping = false;
    [SerializeField] private bool shakeFadeOut = true;
    [SerializeField] private ShakeRandomnessMode randomnessMode = ShakeRandomnessMode.Harmonic; 
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void ShakeSprite()
    {
        bodyObj.transform.DOShakePosition(shakeDuration, shakeStrength, 1,shakeRandomness,shakeSnapping,shakeFadeOut,ShakeRandomnessMode.Harmonic);
    }
}
