using Characters.CollectItemSystems.CollectableItems;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.CollectItemSystems
{
    public class CollectItemSystem : MonoBehaviour
    {
        #region Inspector & Variables
        
        [PropertyTooltip("")]
        [SerializeField] private float collectItemRadius;
        
        [PropertyTooltip("")]
        [SerializeField] private float pullItemRadius;
        
        [PropertyTooltip("")]
        [SerializeField] private LayerMask collectLayer;
        
        #endregion

        #region Unity Methods
        
        /// <summary>
        /// 
        /// </summary>
        private void FixedUpdate()
        {
            Collider2D[] objectsDetected = Physics2D.OverlapCircleAll(transform.position, collectItemRadius, collectLayer);
            
            foreach (Collider2D obj in objectsDetected)
            {
                if (!TryGetComponent(out BaseCollectableItem item)) continue;
                item.PullToTarget(transform);
                if (Vector2.Distance(item.transform.position, transform.position) > collectItemRadius) continue;
                CollectItem(item);
            }
        }
        
        #endregion

        #region Methods
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        private void CollectItem(BaseCollectableItem item)
        {
            item?.HandleOnCollect(this);          
        }
        
        #endregion
    }
}
