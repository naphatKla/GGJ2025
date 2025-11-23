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

        [Title("Volume Curve")]
        [Tooltip("ยิ่งมาก ยิ่งหนักไปทางขวา (ละเอียดฝั่งดัง) น้อยกว่า 1 จะหนักซ้าย")]
        [Range(0.3f, 3f)]
        public float volumeCurve = 1;

        // dB ต่ำสุด (ไม่ต้องเงียบสนิทเกินไป เดี๋ยวครึ่งนึงหายเลย)
        const float MIN_DB = -80f;

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

        // -------- จากสไลเดอร์ (0–1) → dB ด้วย curve --------

        public void UpdateMasterVolume(float value01) => SetMixerVolume("MasterVolume", value01);
        public void UpdateMusicVolume(float value01)  => SetMixerVolume("MusicVolume", value01);
        public void UpdateSfxVolume(float value01)    => SetMixerVolume("SfxVolume", value01);
        public void UpdateUiVolume(float value01)     => SetMixerVolume("UiVolume", value01);

        private void SetMixerVolume(string parameter, float slider01)
        {
            slider01 = Mathf.Clamp01(slider01);

            // ปรับโค้งให้เปลี่ยนชัดขึ้นฝั่งขวา (แก้หนักซ้าย)
            float t = Mathf.Pow(slider01, volumeCurve);   // volumeCurve > 1 = ละเอียดฝั่งดัง

            // แปลงเป็น dB แบบ linear ในช่วง MIN_DB → 0
            float dB = Mathf.Lerp(MIN_DB, 0f, t);

            audioMixer.SetFloat(parameter, dB);
        }

        // -------- Save / Load ค่า slider (0–1) --------

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
