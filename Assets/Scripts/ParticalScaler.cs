using UnityEngine;

public class ParticleSizeScaler : MonoBehaviour
{
    public Transform parent;
    public float scaleMultiplier = 0.15f;
    private ParticleSystem particleSystem;

    void Start()
    {
        // ดึง ParticleSystem ที่แนบมากับ GameObject นี้
        particleSystem = GetComponent<ParticleSystem>();
        if (!parent) parent = transform.parent;
    }

    void Update()
    {
        if (particleSystem != null)
        {
            // ปรับขนาดของ Particle System ให้ตรงกับ scale ของ GameObject
            var mainModule = particleSystem.main;
            mainModule.startSize =
                parent.localScale.x * scaleMultiplier; // ใช้ localScale.x หรือสามารถเลือกใช้ Y, Z ขึ้นอยู่กับการตั้งค่า
        }
    }
}