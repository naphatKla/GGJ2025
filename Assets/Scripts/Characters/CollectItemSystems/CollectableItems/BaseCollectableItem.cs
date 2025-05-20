using Characters.MovementSystems;
using DG.Tweening;
using UnityEngine;

namespace Characters.CollectItemSystems.CollectableItems
{
    public abstract class BaseCollectableItem : MonoBehaviour
    {
        #region Inspectors & Fields
        
        [SerializeField] private RigidbodyMovementSystem rbMovement;
        protected Tween pullingTween;
        
        #endregion

        #region AbstractMethods

        public abstract void PullToTarget(Transform target);
        protected abstract void OnCollect(CollectItemSystem collectItemSystem);

        #endregion

        #region Methods

        public virtual void HandleOnCollect(CollectItemSystem collectItemSystem)
        {
            OnCollect(collectItemSystem);
            gameObject.SetActive(false);
            ResetCollectableItem();
        }
        
        protected virtual void ResetCollectableItem()
        {
            pullingTween?.Kill();
        }
        
        #endregion
    }

    public abstract class BaseCollectableItem<T> : BaseCollectableItem where T : BaseCollectableItemDataSo
    {
        private T _itemData;
        
        public override void PullToTarget(Transform target)
        {
           
        }

        protected override void OnCollect(CollectItemSystem collectItemSystem)
        {
            
        }
    }
}
