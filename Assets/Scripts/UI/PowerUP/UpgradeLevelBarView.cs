using UnityEngine;
using PixelUI;   // namespace ของ SlotBar

public class UpgradeLevelBarView : MonoBehaviour
{
    [SerializeField] private SlotBar slotBar;

    private int _maxLevel;

    /// <summary>เรียกตอน init การ์ด: กำหนดจำนวนช่องสูงสุด + level เริ่มต้น</summary>
    public void Init(int maxLevel, int currentLevel)
    {
        if (slotBar == null) return;

        _maxLevel = Mathf.Max(0, maxLevel);

        slotBar.MinSlots   = 0;
        slotBar.MaxSlots   = _maxLevel;
        slotBar.CurrentSlots = Mathf.Clamp(currentLevel, 0, _maxLevel);

        // ให้มันสร้างช่องตาม MaxSlots หนึ่งรอบ
        slotBar.UpdateSlots();
    }

    /// <summary>อัพเดต level ปัจจุบัน (0..max)</summary>
    public void SetLevel(int level)
    {
        if (slotBar == null || _maxLevel <= 0) return;

        level = Mathf.Clamp(level, 0, _maxLevel);
        slotBar.CurrentSlots = level;   // มันจะไปเรียก Heal()/Damage slot ให้เอง
    }
}