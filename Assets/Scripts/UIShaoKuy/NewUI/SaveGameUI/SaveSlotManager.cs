using System.Collections.Generic;
using UnityEngine;

public class SaveSlotManager : MonoBehaviour
{
    public static SaveSlotManager Instance;

    private SaveSlotUI currentActiveSlot;

    private void Awake()
    {
        Instance = this;
    }

    public void SetActiveSlot(SaveSlotUI slot)
    {
        // ถ้ามีอันก่อนหน้า → ปิดมันก่อน
        if (currentActiveSlot != null && currentActiveSlot != slot)
        {
            currentActiveSlot.DeactivateSlot();
        }

        currentActiveSlot = slot;
    }
}