using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Config ของ Upgrade 1 ช่อง (แก้ใน Inspector)</summary>
[Serializable]
public class UpgradeItemConfig
{
    [Header("Identity")]
    public string id;                 // เช่น "MaxHealth", "Speed"

    [Header("Display")]
    public string displayName;        // ชื่อโชว์ด้านขวา
    [TextArea] public string description;
    public Sprite icon;

    [Header("Level & Cost")]
    public int maxLevel = 4;          // จำนวนช่องใน LevelBar
    public int baseCost = 100;        // ราคาเริ่มต้น
    public bool startLocked;
}

/// <summary>ผูกการ์ดซ้าย (view) กับ config ของมัน</summary>
[Serializable]
public class UpgradeSlot
{
    public UpgradeItemView view;          // ลากการ์ด MaxHealth / Speed ฯลฯ มาใส่
    public UpgradeItemConfig config;      // ตั้งค่า id / ชื่อ / desc / icon / level
}

/// <summary>
/// ตัวกลางของหน้าจอ Upgrade:
/// - Init การ์ดซ้ายทุกใบจาก config
/// - รับคลิกจากการ์ด → highlight + อัปเดต panel ขวา
/// - มีฟังก์ชันให้ปุ่ม COST เรียกอัพเลเวล
/// </summary>
public class UpgradeScreenController : MonoBehaviour
{
    [Header("Fixed slots on the left")]
    [SerializeField] private List<UpgradeSlot> slots = new();

    [Header("Detail panel on the right")]
    [SerializeField] private UpgradeDetailView detailView;

    [Header("Debug Current Points (จะไปผูกกับ PlayerData ภายหลัง)")]
    [SerializeField] private int currentGold = 9999;

    private readonly Dictionary<string, int> _levels = new();   // level ต่อ id
    private int _selectedIndex = -1;

    private void Start()
    {
        InitSlots();

        if (detailView != null)
            detailView.Clear();
    }

    /// <summary>ตั้งค่าเริ่มต้นให้ทุก slot</summary>
    private void InitSlots()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            var view = slot.view;
            var cfg  = slot.config;

            if (view == null || cfg == null)
            {
                Debug.LogWarning($"[UpgradeScreen] Slot {i} missing view or config.");
                continue;
            }

            int level = GetLevel(cfg.id);
            Debug.Log($"[UpgradeScreen] Init slot {i} ({cfg.id}) level={level}");

            view.SetIcon(cfg.icon);
            view.SetLocked(cfg.startLocked);
            view.InitLevelBar(cfg.maxLevel, level);
            view.SetSelected(false);

            int index = i; // ป้องกัน closure
            view.OnClick += () =>
            {
                Debug.Log($"[UpgradeScreen] OnClick from view {view.name} index={index}");
                OnSlotClicked(index);
            };
        }
    }

    private int GetLevel(string id)
    {
        if (string.IsNullOrEmpty(id)) return 0;
        return _levels.TryGetValue(id, out var lvl) ? lvl : 0;
    }

    /// <summary>ถูกเรียกเมื่อคลิกการ์ดช่องใดช่องหนึ่ง</summary>
    private void OnSlotClicked(int index)
    {
        Debug.Log($"[UpgradeScreen] OnSlotClicked index={index}");

        if (index < 0 || index >= slots.Count)
            return;

        // clear select เดิม
        if (_selectedIndex >= 0 && _selectedIndex < slots.Count)
        {
            var prevView = slots[_selectedIndex].view;
            if (prevView != null)
                prevView.SetSelected(false);
        }

        _selectedIndex = index;
        var slot = slots[index];

        if (slot.view != null)
        {
            Debug.Log($"[UpgradeScreen] SetSelected TRUE on {slot.view.name}");
            slot.view.SetSelected(true);
        }

        var cfg = slot.config;
        if (cfg == null || detailView == null)
            return;

        int level = GetLevel(cfg.id);
        int cost  = CalcCost(cfg, level);

        detailView.Show(cfg, level, cost);
    }

    /// <summary>สูตรคำนวณราคาแบบง่าย ๆ (เปลี่ยนทีหลังได้)</summary>
    private int CalcCost(UpgradeItemConfig cfg, int currentLevel)
    {
        return cfg.baseCost + currentLevel * 50;
    }

    /// <summary>
    /// ให้ปุ่ม COST ด้านขวาเรียกฟังก์ชันนี้ตอน OnClick
    /// </summary>
    public void OnClickUpgradeButton()
    {
        if (_selectedIndex < 0 || _selectedIndex >= slots.Count)
        {
            Debug.Log("[Upgrade] No slot selected.");
            return;
        }

        var slot = slots[_selectedIndex];
        var cfg  = slot.config;

        if (cfg == null)
        {
            Debug.LogWarning("[Upgrade] Selected slot has no config.");
            return;
        }

        int currentLevel = GetLevel(cfg.id);

        if (currentLevel >= cfg.maxLevel)
        {
            Debug.Log($"[Upgrade] {cfg.id} already at max level ({currentLevel}).");
            return;
        }

        int cost = CalcCost(cfg, currentLevel);

        if (currentGold < cost)
        {
            Debug.Log($"[Upgrade] Not enough points. Need {cost}, have {currentGold}.");
            return;
        }

        // หักแต้ม + เพิ่มเลเวล
        currentGold -= cost;
        int newLevel = currentLevel + 1;
        _levels[cfg.id] = newLevel;

        if (slot.view != null)
            slot.view.SetLevel(newLevel);

        if (detailView != null)
        {
            int newCost = CalcCost(cfg, newLevel);
            detailView.Show(cfg, newLevel, newCost);
        }

        Debug.Log($"[Upgrade] Upgraded {cfg.id} to level {newLevel}. Cost {cost}, remain {currentGold}.");
    }

    /// <summary>
    /// ใช้จากระบบอื่น ถ้าต้องการ set level จากภายนอก (เช่น โหลดเซฟ)
    /// </summary>
    public void SetLevel(string id, int newLevel)
    {
        if (string.IsNullOrEmpty(id)) return;

        _levels[id] = newLevel;

        // sync ไปยังการ์ดซ้าย
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot.config == null || slot.view == null) continue;
            if (slot.config.id != id) continue;

            slot.view.SetLevel(newLevel);
        }

        // ถ้า panel ขวาโชว์อันนี้อยู่ ให้ update ด้วย
        if (_selectedIndex >= 0 && _selectedIndex < slots.Count)
        {
            var slot = slots[_selectedIndex];
            if (slot.config != null && slot.config.id == id && detailView != null)
            {
                int cost = CalcCost(slot.config, newLevel);
                detailView.Show(slot.config, newLevel, cost);
            }
        }
    }
}
