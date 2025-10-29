using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace UI.SettingPanel
{
    public class SettingManager : MonoBehaviour
    {
        public AudioMixer audioMixer;
        
        [Title("Slider")]
        public Slider masterSlider;
        public Slider musicSlider;
        public Slider sfxSlider;
        public Slider uiSlider;

        private void Start()
        {
            LoadVolume();
        }
        
        private void OnEnable()
        {
            LoadVolume();
        }
        
        private void OnDisable()
        {
            SaveVolume();
        }

        public void UpdateMasterVolume(float volume)
        {
            audioMixer.SetFloat("MasterVolume", volume);
        }
 
        public void UpdateMusicVolume(float volume)
        {
            audioMixer.SetFloat("MusicVolume", volume);
        }
        
        public void UpdateSfxVolume(float volume)
        {
            audioMixer.SetFloat("SfxVolume", volume);
        }
        
        public void UpdateUiVolume(float volume)
        {
            audioMixer.SetFloat("UiVolume", volume);
        }
        
        public void SaveVolume()
        {
            audioMixer.GetFloat("MasterVolume", out float masterVolume);
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            
            audioMixer.GetFloat("MusicVolume", out float musicVolume);
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
            
            audioMixer.GetFloat("SfxVolume", out float sfxVolume);
            PlayerPrefs.SetFloat("SfxVolume", sfxVolume);
            
            audioMixer.GetFloat("UiVolume", out float uiVolume);
            PlayerPrefs.SetFloat("UiVolume", uiVolume);
        }
        
        public void LoadVolume()
        {
            masterSlider.value = PlayerPrefs.GetFloat("MasterVolume");
            uiSlider.value = PlayerPrefs.GetFloat("UiVolume");
            musicSlider.value = PlayerPrefs.GetFloat("MusicVolume");
            sfxSlider.value = PlayerPrefs.GetFloat("SfxVolume");
        }
    }
}