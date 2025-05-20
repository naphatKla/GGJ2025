using Characters.CollectItemSystems;
using Characters.MovementSystems;
using DG.Tweening;
using UnityEngine;

namespace CollectableItems
{
    public abstract class BaseCollectableItem : MonoBehaviour
    {
        #region Inspectors & Fields
        
        [SerializeField] private RigidbodyMovementSystem rbMovement;
        protected Tween pullingTween;
        
        #endregion

        #region AbstractMethods

        public abstract void PullToTarget(Transform target);
        protected abstract void OnCollect(BaseController baseController);

        #endregion

        #region Methods

        public virtual void HandleOnCollect(BaseController baseController)
        {
            OnCollect(baseController);
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
            throw new System.NotImplementedException();
        }

        protected override void OnCollect(BaseController baseController)
        {
            throw new System.NotImplementedException();
        }
    }
}
