using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;


namespace UI.IngameViewholder
{
    public class FlowStateComboViewHolder : SerializedMonoBehaviour
    {
        [InfoBox("เขียน String ให้ตรงเช่น flow_i")]
        public Dictionary<string, GameObject> FlowStateDic;

        GameObject _current;

        public void UpdateFlowSceneFeedback(string flowID)
        {
            if (_current != null) _current.SetActive(false);
            if (FlowStateDic.TryGetValue(flowID, out _current))
            {
                _current.SetActive(true);
                EntryFeedback();
            }
        }
        private void EntryFeedback()
        {
            if (_current == null) return;

            var t = _current.transform;
            t.localScale = new Vector3(1.25f, 1f, 1f);
            t.DOKill();

            Sequence seq = DOTween.Sequence();
            seq.Append(t.DOScaleX(0.95f, 1f).SetEase(Ease.InOutSine))
                .Append(t.DOScaleX(1f, 1f).SetEase(Ease.OutBack));
        }

        public void CloseCurrent()
        {
            _current?.gameObject.SetActive(false);
        }
    }
}
