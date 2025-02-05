using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;


public class AudioSetting : MonoBehaviour
{
    #region Inspector Variables
    [SerializeField] private AudioMixer _mixer;
    [SerializeField] private SerializableDictionary<Slider, AudioSO> audioSlider;
    #endregion

    #region MyRegion
    private void Awake()
    {
        SetMusicVolume();
        foreach (var slider in audioSlider)
        {
            slider.Key.minValue = 0.0001f;
            slider.Key.value = slider.Value.volume;
        }
    }

    private void Start()
    {
        SetMusicVolume();
    }
    
    private void Update()
    {
        foreach (var audio in audioSlider)
        {
            audio.Value.volume = audio.Key.value;
        }
    }

    public void SetMusicVolume()
    {
        foreach (var slider in audioSlider)
        {
            _mixer.SetFloat(slider.Value.audioName, Mathf.Log10(slider.Key.value)*20);
        }
        
    }
    #endregion
}