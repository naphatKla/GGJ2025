using System;
using System.Collections.Generic;
using Achievements;
using Demo;
using Player;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Achievement
{
    [RequireComponent(typeof(LoopScrollRect))]
    [DisallowMultipleComponent]
    public class AchievementInitOnStart : MonoBehaviour, LoopScrollPrefabSource, LoopScrollDataSource
    {
        [Space,Title("Setting")]
        public GameObject item;
        public int totalCount = -1;
        
        [Space,Title("Container")]
        [SerializeField] public AchievementDatabase achievementDataContainer;

        [Space, Title("UI")] 
        public TMP_Text completeAchievement;
        
        [Space,Title("Debug")]
        [ShowInInspector] public int m_SelectedIndex = -1;
        [ShowInInspector] public AchievementEntry m_SelectedObject;
        
        Stack<Transform> pool = new Stack<Transform>();
        private List<AchievementEntry> _items = new();
        private PlayerData Current => ActiveProfileService.Instance != null ? ActiveProfileService.Instance.CurrentProfile : null;
        
        void Awake()
        {
            _items = LoadItems() ?? new List<AchievementEntry>();
            totalCount = _items.Count;
        }
        
        void Start()
        {
            var ls = GetComponent<LoopScrollRect>();
            ls.prefabSource = this;
            ls.dataSource = this;
            ls.totalCount = totalCount;
            ls.RefillCells();
            ls.RefreshCells();
        }

        private void OnEnable()
        {
            var ls = GetComponent<LoopScrollRect>();
            ls.RefillCells();
            ls.RefreshCells();
            UpdateCompleteText();
        }

        private void UpdateCompleteText()
        {
            if (completeAchievement != null)
            {
                var allachievement = achievementDataContainer.entries.Count;
                var complete = Current.UnlockedAchievements.Count;
                var percent = ((float)complete / allachievement) * 100;
                completeAchievement.text = $"COMPLETED <color=#fdb520>{complete}/{allachievement}</color> | TOTAL <color=#00a86b>{percent:F0}%</color>";
            }
        }

        private List<AchievementEntry> LoadItems()
        {
            // copy list จาก database กัน side-effect
            var list = new List<AchievementEntry>(achievementDataContainer.entries);

            if (Current != null)
            {
                list.Sort((a, b) =>
                {
                    bool aComplete = Current.UnlockedAchievements.Contains(a.id);
                    bool bComplete = Current.UnlockedAchievements.Contains(b.id);

                    // ยังไม่ complete (aComplete == false) ให้อยู่ก่อน
                    if (aComplete == bComplete)
                    {
                        // ถ้าอยากจัดต่อด้วยชื่อ/ลำดับ ก็ใส่ตรงนี้
                        return string.Compare(a.id, b.id, StringComparison.Ordinal);
                    }

                    // false ก่อน true  -> ไม่ complete อยู่บน, complete อยู่ล่าง
                    return aComplete ? 1 : -1;
                });
            }

            return list;
        }

        
        private bool IsLockedByPlayer(AchievementEntry achievementEntry)
        {
            if (Current == null || achievementEntry == null) return false;
            return !Current.UnlockedAchievements.Contains(achievementEntry.id);
        }
        
        public GameObject GetObject(int index)
        {
            var go = pool.Count == 0 ? Instantiate(item) : pool.Pop().gameObject;
            go.SetActive(true);
            var cb = go.GetComponent<ScrollIndexCallbackBase>();
            if (cb != null)
            {
                cb.onClick_InitOnStart.RemoveAllListeners();
            }
            return go;
        }

        public void ReturnObject(Transform trans)
        {
            trans.SendMessage("ScrollCellReturn", SendMessageOptions.DontRequireReceiver);
            trans.gameObject.SetActive(false);
            trans.SetParent(transform, false);
            pool.Push(trans);
        }

        public void ProvideData(Transform transform, int idx)
        {
            var vh = transform.GetComponent<AchievementViewholder>();
            if (vh != null)
            {
                var content = _items[idx];
                var p = Current;
                vh.SetPrefabName("Achievement");   
                vh.ScrollCellIndex(idx, content);
                vh.IsLocked = IsLockedByPlayer(content);
                vh.UpdateViewholder(_items[idx], p);
            }
            else
            {
                transform.SendMessage("ScrollCellIndex", idx, SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}
