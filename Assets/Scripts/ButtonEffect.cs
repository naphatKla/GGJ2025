using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    List<GameObject> buttons = new List<GameObject>();
    public Vector3 hoverScale = new Vector3(1.2f, 1.2f, 1.2f); // ขนาดเมื่อชี้เมาส์
    private Vector3 originalScale; // ขนาดเดิมของปุ่ม

    private void Start()
    {
        // บันทึกขนาดเริ่มต้นของปุ่ม
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // เพิ่มขนาดเมื่อเมาส์ชี้
        transform.localScale = hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // กลับสู่ขนาดเดิมเมื่อเมาส์ออก
        transform.localScale = originalScale;
    }
}