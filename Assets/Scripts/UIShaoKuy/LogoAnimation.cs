using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogoAnimation : MonoBehaviour
{
    public Transform logo;
    public float minScale = 6f;
    public float maxScale = 8f;
    public float animationSpeed = 0.2f;

    private float timer = 0f;

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
