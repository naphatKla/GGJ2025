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

        const float MIN_DB = -80f;   // ค่าเงียบ

        private void Start()
        {
            LoadVolume();
            ApplyAllVolumes();
        }

        private void OnEnable()
        {
            LoadVolume();
            ApplyAllVolumes();
        }

        private void OnDisable()
        {
            SaveVolume();
        }

        // ---------- ปรับจาก slider (0–1) เป็น dB ----------

        public void UpdateMasterVolume(float value01)
        {
            SetMixerVolume("MasterVolume", value01);
        }

        public void UpdateMusicVolume(float value01)
        {
            SetMixerVolume("MusicVolume", value01);
        }

        public void UpdateSfxVolume(float value01)
        {
            SetMixerVolume("SfxVolume", value01);
        }

        public void UpdateUiVolume(float value01)
        {
            SetMixerVolume("UiVolume", value01);
        }

        private void SetMixerVolume(string parameter, float value01)
        {
            // value01 = 0..1 จาก slider
            float dB;

            if (value01 <= 0.0001f)
                dB = MIN_DB;                 // เงียบ
            else
                dB = Mathf.Log10(value01) * 20f;  // แปลงเป็น dB

            audioMixer.SetFloat(parameter, dB);
        }

        // ---------- Save / Load ----------

        public void SaveVolume()
        {
            PlayerPrefs.SetFloat("MasterVolume", masterSlider.value);
            PlayerPrefs.SetFloat("MusicVolume",  musicSlider.value);
            PlayerPrefs.SetFloat("SfxVolume",    sfxSlider.value);
            PlayerPrefs.SetFloat("UiVolume",     uiSlider.value);
            PlayerPrefs.Save();
        }

        public void LoadVolume()
        {
            masterSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
            musicSlider.value  = PlayerPrefs.GetFloat("MusicVolume",  1f);
            sfxSlider.value    = PlayerPrefs.GetFloat("SfxVolume",    1f);
            uiSlider.value     = PlayerPrefs.GetFloat("UiVolume",     1f);
        }

        private void ApplyAllVolumes()
        {
            UpdateMasterVolume(masterSlider.value);
            UpdateMusicVolume(musicSlider.value);
            UpdateSfxVolume(sfxSlider.value);
            UpdateUiVolume(uiSlider.value);
        }
    }
}
