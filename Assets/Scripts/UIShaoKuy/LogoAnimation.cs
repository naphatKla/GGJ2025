using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogoAnimation : MonoBehaviour
{
    public Transform logo; // Drag โลโก้ของคุณใน Inspector
    public float minScale = 0.8f; // ขนาดต่ำสุด
    public float maxScale = 1.2f; // ขนาดสูงสุด
    public float animationSpeed = 1f; // ความเร็วของ Animation

    private float timer = 0f; // ตัวนับเวลา

    void Update()
    {
        if (logo != null)
        {
            // คำนวณค่า Scale ที่ราบรื่นด้วย PingPong
            float scale = Mathf.Lerp(minScale, maxScale, Mathf.PingPong(timer * animationSpeed, 1f));
            logo.localScale = new Vector3(scale, scale, scale);

            // อัปเดตตัวนับเวลา
            timer += Time.deltaTime;
        }
    }
}
