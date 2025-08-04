using System;
using Characters.MovementSystems;
using Characters.SO.CollectableItemDataSO;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GlobalSettings;
using ProjectExtensions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Characters.CollectItemSystems.CollectableItems
{
    /// <summary>
    /// Abstract base class for all collectible items.
    /// Provides a shared interface for assigning item data, handling collection logic, and pulling the item toward a target.
    /// </summary>
    public abstract class BaseCollectableItem : MonoBehaviour
    {
        #region Inspector & Variables

        /// <summary>
        /// Event invoked when this item is collected by a collector.
        /// The <see cref="GameObject"/> parameter represents the item GameObject that was collected.
        /// </summary>
        public Action OnThisItemCollected { get; set; }
        
        #endregion
        
        #region AbstractMethods
        
        /// <summary>
        /// Begins pulling the item toward a target transform with optional callback upon completion.
        /// </summary>
        /// <param name="target">The target to pull toward (e.g., the player).</param>
        /// <param name="callback">Callback that fires when the pull completes.</param>
        public abstract void PullToTarget(Transform target, TweenCallback callback);

        /// <summary>
        /// Called when the item is successfully collected by a collector system.
        /// </summary>
        /// <param name="ownerSystem">The system or character that triggered the collection.</param>
        public abstract void HandleOnCollect(CollectItemSystem ownerSystem);

        #endregion
    }

    /// <summary>
    /// Generic base class for collectable items with strongly typed ScriptableObject data.
    /// Manages setup, sprite rendering, physics, movement, and pulling behavior.
    /// </summary>
    /// <typeparam name="T">The type of ScriptableObject used for this item (inherits from BaseCollectableItemDataSo).</typeparam>
    public abstract class BaseCollectableItem<T> : BaseCollectableItem where T : BaseCollectableItemDataSo
    {
        #region Inspector & Variables

        /// <summary>
        /// The assigned data for this item instance, cast to the specific type.
        /// </summary>
        [SerializeField]
        protected T itemData;

        /// <summary>
        /// The movement system used to perform tween-based pull motion.
        /// </summary>
        [SerializeField]
        protected RigidbodyMovementSystem rbMovementSystem;
        
        /// <summary>
        /// Tween instance used to pull the item toward the target.
        /// Prevents multiple simultaneous pulls.
        /// </summary>
        private Tween _pullingTween;

        /// <summary>
        /// Flag indicating whether this item can currently be collected.
        /// Set to false on spawn and enabled after the pickup delay expires.
        /// </summary>
        private bool _canCollect;

        public T ItemData => itemData;

        #endregion

        #region Unity Methods

        /// <summary>
        /// Called when the item is enabled in the scene.
        /// Starts a delay timer before allowing collection, based on <see cref="itemData.PickupDelay"/>.
        /// </summary>
        private async void OnEnable()
        {
            await UniTask.WaitUntil(() => itemData != null);
            await UniTask.WaitForSeconds(itemData.PickupDelay);
            _canCollect = true;
        }

        #endregion
        
        #region Abstract Methods

        /// <summary>
        /// Defines what happens when the item is collected (e.g., reward logic, feedback).
        /// Must be implemented by child classes.
        /// </summary>
        /// <param name="ownerSystem">The system that collected this item.</param>
        protected abstract void OnCollect(CollectItemSystem ownerSystem);

        #endregion

        #region Methods
        
        /// <summary>
        /// Triggers item collection logic and resets the item state.
        /// </summary>
        /// <param name="ownerSystem">The system that collected this item.</param>
        public override void HandleOnCollect(CollectItemSystem ownerSystem)
        {
            OnCollect(ownerSystem);
            OnThisItemCollected?.Invoke();
            gameObject.SetActive(false);
            ResetItem();
        }

        /// <summary>
        /// Begins pulling the item toward the specified target over time.
        /// Tweening parameters are controlled by the assigned item data.
        /// </summary>
        /// <param name="target">The target transform to pull toward.</param>
        /// <param name="callback">Callback executed when the pull completes.</param>
        public override void PullToTarget(Transform target, TweenCallback callback)
        {
            if (Vector2.Distance(transform.position, target.position) <= 0.35f) // Pickup threshold
                _pullingTween.Kill(true);
                
            if (_pullingTween.IsActive()) return;
            if (!_canCollect) return;

            float duration = Random.Range(itemData.PullDuration.x, itemData.PullDuration.y);
            _pullingTween = rbMovementSystem
                .TryMoveToTargetOverTime(target, duration, itemData.PullEase, itemData.PullCurve)
                .OnComplete(callback);
        }

        /// <summary>
        /// Kills the active pull tween and prepares the item for reuse or pooling.
        /// </summary>
        public virtual void ResetItem()
        {
            _pullingTween?.Kill();
            _canCollect = false;
        }

        #endregion
    }
}
