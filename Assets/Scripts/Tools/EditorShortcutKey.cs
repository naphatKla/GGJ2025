#if UNITY_EDITOR
using System.Collections.Generic;
using Characters.Controllers;
using Characters.LevelSystems;
using Characters.SkillSystems;
using GameControl;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tools
{
    public enum EditorAction
    {
        LevelUp = 0,
        ForceDie = 1,
        ReduceTime10Sec = 2,
        UpgradeMax = 3,
    }

    public class EditorShortcutKey : SerializedMonoBehaviour
    {
        [DictionaryDrawerSettings] [SerializeField]
        private Dictionary<KeyCode, EditorAction> KeyMap;

        void Update()
        {
            if (KeyMap == null || KeyMap.Count == 0) return;

            // กันกดคีย์ตอนกำลังพิมพ์ใน InputField/TMP_InputField
            if (IsTypingInUI()) return;

            // เช็คเฉพาะเฟรมที่มีการ "กดลง" อย่างน้อยหนึ่งปุ่ม
            if (!Input.anyKeyDown) return;

            // วนตามแผนที่คีย์ → แอคชัน
            foreach (var kv in KeyMap)
            {
                // ถ้าต้องการให้กดค้างก็เปลี่ยนเป็น GetKey()
                if (Input.GetKeyDown(kv.Key))
                {
                    PerformAction(kv.Value);
                    return; // จัดการครั้งละ 1 แอคชันพอ
                }
            }
        }

// ===== Helpers =====
        private static bool IsTypingInUI()
        {
            var es = EventSystem.current;
            if (es == null) return false;

            var sel = es.currentSelectedGameObject;
            if (sel == null) return false;

            // มี InputField/TMP_InputField โฟกัสอยู่ → งดจับช็อตคัต
            return sel.GetComponent<TMP_InputField>() != null ||
                   sel.GetComponent<InputField>() != null;
        }

        private void PerformAction(EditorAction keycode)
        {
            switch (keycode)
            {
                case EditorAction.LevelUp:
                {
                    PlayerController.Instance.LevelSystem.ForceLevelUp();
                    break;
                }
                case EditorAction.ForceDie:
                {
                    PlayerController.Instance.HealthSystem.TakeDamage(PlayerController.Instance.HealthSystem.MaxHealth, out bool _);
                    break;
                }
                case EditorAction.ReduceTime10Sec:
                {
                    GameTimer.Instance.SkipTime(10);
                    break;
                }
                case EditorAction.UpgradeMax:
                {
                    PlayerController.Instance.GetComponent<SkillUpgradeController>().Dev_UpgradeAllToMaxNow();
                    break;
                }
            }
        }
    }
}
#endif