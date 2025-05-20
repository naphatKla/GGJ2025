using Characters.CollectItemSystems.CollectableItems;
using DG.Tweening;
using GlobalSettings;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.CollectItemSystems
{
    public class CollectItemSystem : MonoBehaviour
    {
        #region Inspector & Variables
        
        [PropertyTooltip("")]
        [SerializeField] private float pullItemRadius;
        
        [PropertyTooltip("")]
        [SerializeField] private bool enableGizmos;

        /// <summary>
        /// 
        /// </summary>
        private LayerMask collectLayer => CharacterGlobalSettings.Instance.CollectableItemLayerMask;
        
        #endregion

        #region Unity Methods
        
        /// <summary>
        /// 
        /// </summary>
        private void FixedUpdate()
        {
            Collider2D[] objectsDetected = Physics2D.OverlapCircleAll(transform.position, pullItemRadius, collectLayer);
            
            foreach (Collider2D obj in objectsDetected)
            {
                if (!obj.TryGetComponent(out BaseCollectableItem item)) continue;
                item.PullToTarget(transform, () => CollectItem(item));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!enableGizmos) return;

            Vector3 position = transform.position;
            Gizmos.color = new Color(0f, 0.6f, 1f, 0.25f); 
            Gizmos.DrawWireSphere(position, pullItemRadius);
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
