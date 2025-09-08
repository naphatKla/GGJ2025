using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace UI
{
    public class ScrollWheelForwarder : MonoBehaviour, IScrollHandler
    {
        [Header("Target (optional)")]
        [Tooltip("ถ้ากำหนดไว้ จะส่งอีเวนต์ไปที่ตัวนี้โดยตรง")]
        public ScrollRect specificTarget;

        [Tooltip("ถ้าไม่กำหนด specificTarget จะหา ScrollRect โดย Raycast ใต้ตำแหน่งเมาส์")]
        public bool findByRaycast = true;

        private static readonly List<RaycastResult> raycastResults = new List<RaycastResult>(16);

        public void OnScroll(PointerEventData eventData)
        {
            if (specificTarget != null)
            {
                ExecuteEvents.Execute<IScrollHandler>(specificTarget.gameObject, eventData, ExecuteEvents.scrollHandler);
                return;
            }
            
            if (!findByRaycast || EventSystem.current == null) return;

            raycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, raycastResults);

            foreach (var rr in raycastResults)
            {
                var go = rr.gameObject;
                if (go == gameObject || go.transform.IsChildOf(transform)) continue;
                var handler = ExecuteEvents.GetEventHandler<IScrollHandler>(go);
                if (handler != null)
                {
                    ExecuteEvents.Execute(handler, eventData, ExecuteEvents.scrollHandler);
                    break;
                }
            }
        }
    } 
}
