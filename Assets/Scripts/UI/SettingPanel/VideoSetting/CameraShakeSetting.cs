using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class CameraShakeSetting : MonoBehaviour
{
    public enum Preset { Low, Medium, High }

    [Header("Buttons")]
    public Button lowButton;
    public Button mediumButton;
    public Button highButton;

    [Header("Preset Intensities")]
    [Range(0f, 2f)] public float low = 0.2f;
    [Range(0f, 2f)] public float medium = 0.45f;
    [Range(0f, 2f)] public float high = 0.8f;

    [Header("Start")]
    public Preset start = Preset.Medium;

    // จุดต่อให้ Dev: ยิงค่า intensity ออกไป
    [System.Serializable] public class FloatEvent : UnityEvent<float>
    {
        public void Invoke(float intensity)
        {
            throw new System.NotImplementedException();
        }
    }
    public FloatEvent OnIntensityChanged;

    public Preset Current { get; private set; }

    void Start() => Apply(start, invoke:false);

    // ========== ปุ่มกด ==========
    public void OnLowClicked()    => Apply(Preset.Low);
    public void OnMediumClicked() => Apply(Preset.Medium);
    public void OnHighClicked()   => Apply(Preset.High);

    // ========== แกนกลาง ==========
    void Apply(Preset p, bool invoke = true)
    {
        Current = p;

        // อัปเดตหน้าตาง่าย ๆ: ปุ่มที่ถูกเลือกกดซ้ำไม่ได้ (ดูออกว่า active)
        /*if (lowButton)    lowButton.interactable    = p != Preset.Low;
        if (mediumButton) mediumButton.interactable = p != Preset.Medium;
        if (highButton)   highButton.interactable   = p != Preset.High;*/

        if (!invoke) return;

        float intensity = GetIntensity(p);

        // ----- จุดต่อให้ Dev เขียนต่อที่นี่ -----
        OnIntensityChanged?.Invoke(intensity);         // (แนะนำ) ผูกไปที่ระบบกล้อง
        // ตัวอย่าง:
        // CameraShakeController.Instance.SetIntensity(intensity);
        // CinemachineImpulseController.SetAmplitude(intensity);
        // PlayerPrefs.SetFloat("video.cameraShake", intensity);
        // ----------------------------------------

        Debug.Log($"CameraShake: {p} ({intensity})");
    }

    float GetIntensity(Preset p)
    {
        switch (p)
        {
            case Preset.Low:    return low;
            case Preset.Medium: return medium;
            case Preset.High:    return high;
            default:             return medium;
        }
    }
}
