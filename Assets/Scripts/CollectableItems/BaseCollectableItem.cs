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
        
    }
}
