using Characters.MovementSystems;
using Characters.SO.CollectableItemDataSO;
using DG.Tweening;
using ProjectExtensions;
using UnityEngine;

namespace Characters.CollectItemSystems.CollectableItems
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class BaseCollectableItem : MonoBehaviour
    {
        #region AbstractMethods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public abstract void AssignItemData(BaseCollectableItem data);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        public abstract void PullToTarget(Transform target);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ownerSystem"></param>
        public abstract void HandleOnCollect(CollectItemSystem ownerSystem);

        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseCollectableItem<T> : BaseCollectableItem where T : BaseCollectableItemDataSo
    {
        #region Inspector & Variables
        
        /// <summary>
        /// 
        /// </summary>
        protected T itemData;
        
        /// <summary>
        /// 
        /// </summary>
        protected RigidbodyMovementSystem rbMovementSystem;

        /// <summary>
        /// 
        /// </summary>
        private SpriteRenderer _spriteRenderer;

        /// <summary>
        /// 
        /// </summary>
        private CircleCollider2D _circleCollider2D;
        
        /// <summary>
        /// 
        /// </summary>
        private Rigidbody2D _rb2D;

        /// <summary>
        /// 
        /// </summary>
        private Tween _pullingTween;
        
        #endregion

        #region Abstract Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ownerSystem"></param>
        protected abstract void OnCollect(CollectItemSystem ownerSystem);

        #endregion
        
        #region Methods
        
        /// <summary>
        /// 
        /// </summary>
        private void InitializeItem()
        {
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            _circleCollider2D = gameObject.AddComponent<CircleCollider2D>();
            _rb2D = gameObject.AddComponent<Rigidbody2D>();
            rbMovementSystem = gameObject.AddComponent<RigidbodyMovementSystem>();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public override void AssignItemData(BaseCollectableItem data)
        {
            InitializeItem();
            itemData = data as T;
            
            _spriteRenderer.sprite = itemData.Icon;
            Collider2DSnapper.SnapPhysicsShape(_spriteRenderer, _circleCollider2D);
            _rb2D.gravityScale = 0f;
            _rb2D.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ownerSystem"></param>
        public override void HandleOnCollect(CollectItemSystem ownerSystem)
        {
            OnCollect(ownerSystem);
            gameObject.SetActive(false);
            ResetItem();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        public override void PullToTarget(Transform target)
        {
            if (_pullingTween.IsActive()) return;
            _pullingTween = rbMovementSystem.TryMoveToTargetOverTime(target, itemData.PullDuration, itemData.PullEase, itemData.PullCurve);
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void ResetItem()
        {
            _pullingTween?.Kill();
        }
        
        #endregion
    }
}
