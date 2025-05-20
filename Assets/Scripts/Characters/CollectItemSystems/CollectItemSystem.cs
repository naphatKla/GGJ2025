using Characters.CollectItemSystems.CollectableItems;
using UnityEngine;

namespace Characters.CollectItemSystems
{
    public class CollectItemSystem : MonoBehaviour
    {
        [SerializeField] private float collectItemRadius;
        [SerializeField] private float pullItemRadius;
        [SerializeField] private LayerMask collectLayer;
        
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

        private void CollectItem(BaseCollectableItem item)
        {
            item?.HandleOnCollect(this);          
        }
    }
}
