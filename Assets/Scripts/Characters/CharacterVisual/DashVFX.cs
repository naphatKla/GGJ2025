using System.Collections;
using System.Collections.Generic;
using Characters.InputSystems;
using UnityEngine;

public class DashVFX : MonoBehaviour
{
   
    [SerializeField] private ParticleSystem particleSystem;
    public float additional = 0f;
        void Update()
        {
            // ตำแหน่งของเมาส์ใน world space
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = 0f; // ลบค่า Z เพราะเป็น 2D

            // คำนวณทิศทางจากวัตถุไปยังเมาส์
            Vector3 direction = mousePosition - transform.position;

            // คำนวณมุมและหมุนวัตถุ
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle+additional);
            
            if (Input.GetMouseButtonDown(0)) // คลิกซ้าย
            {
                if (particleSystem != null)
                {
                    particleSystem.Play();
                }
            }
        }
}
