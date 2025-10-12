using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Leaderboard
{
    [RequireComponent(typeof(LoopScrollRect))]
    public class LeaderboardItemPresenter : MonoBehaviour, LoopScrollPrefabSource, LoopScrollDataSource
    {
        public GameObject itemPrefab;
        
        private readonly List<LeaderboardItemModel> _items = new List<LeaderboardItemModel>();
        private readonly Stack<Transform> _pool = new Stack<Transform>();

        private LoopScrollRect _ls;
        
        void Awake()
        {
            _ls = GetComponent<LoopScrollRect>();
            for (int i = 0; i < 200; i++)
            {
                _items.Add(new LeaderboardItemModel($"NAME", 99999));
            }
            
            _ls.prefabSource = this;
            _ls.dataSource   = this;
            _ls.totalCount = _items.Count;
            _ls.RefillCells();
        }
        
        public GameObject GetObject(int index)
        {
            if (_pool.Count == 0)
                return Instantiate(itemPrefab);

            var tr = _pool.Pop();
            tr.gameObject.SetActive(true);
            return tr.gameObject;
        }

        public void ReturnObject(Transform trans)
        {
            var view = trans.GetComponent<LeaderboardItemViewholder>();
            if (view != null) view.OnCellReturn();

            trans.gameObject.SetActive(false);
            trans.SetParent(transform, false);
            _pool.Push(trans);
        }

        public void ProvideData(Transform transform, int idx)
        {
            var view = transform.GetComponent<LeaderboardItemViewholder>();
            if (view == null) return;

            var data = _items[idx];
            view.SetData(data, idx);
        }
        
        public void UpdateItem(int index, LeaderboardItemModel newData)
        {
            if (index < 0 || index >= _items.Count) return;
            _items[index] = newData;
            _ls.RefreshCells();
        }
        
        public void AddItems(IEnumerable<LeaderboardItemModel> more)
        {
            _items.AddRange(more);
            _ls.totalCount = _items.Count;
            _ls.RefreshCells();
        }
        
        public void ResetItems(List<LeaderboardItemModel> newList, bool refill = true)
        {
            _items.Clear();
            _items.AddRange(newList);
            _ls.totalCount = _items.Count;
            if (refill) _ls.RefillCells(); else _ls.RefreshCells();
        }
    }

}
