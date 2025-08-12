using System;
using Characters.CollectItemSystems.CollectableItems;
using Characters.Controllers;
using GlobalSettings;
using Manager;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.CollectItemSystems
{
    /// <summary>
    /// Handles automatic detection and collection of nearby items within a defined radius.
    /// Uses Unity Physics2D to detect items on the defined collectible layer and pulls them toward the player.
    /// When close enough, the item is collected through its assigned collect logic.
    /// </summary>
    public class CollectItemSystem : MonoBehaviour, IFixedUpdateable
    {
        #region Inspector & Variables
        
        /// <summary>
        /// The LayerMask used to detect which objects are considered collectible.
        /// This value is pulled from global character settings.
        /// </summary>
        private LayerMask collectLayer => CharacterGlobalSettings.Instance.CollectableItemLayerMask;
        private float _pullItemRadius;
        private BaseController _owner;
        public BaseController Owner => _owner;

        #endregion

        #region Unity Methods

        private void OnEnable()
        {
            FixedUpdateManager.Instance.Register(this);
        }

        private void OnDisable()
        {
            FixedUpdateManager.Instance.Unregister(this);
        }

        public void OnFixedUpdate()
        {
            Collider2D[] objectsDetected = Physics2D.OverlapCircleAll(transform.position, _pullItemRadius, collectLayer);

            foreach (Collider2D obj in objectsDetected)
            {
                if (!obj.TryGetComponent(out BaseCollectableItem item)) continue;
                item.PullToTarget(transform, () => CollectItem(item));
            }
        }
        
        #endregion

        #region Methods

        public void AssignData(BaseController owner, float pullItemRadius)
        {
            _owner = owner;
            _pullItemRadius = pullItemRadius;
        }
        
        /// <summary>
        /// Triggers the collection logic on the specified item.
        /// </summary>
        /// <param name="item">The item to be collected.</param>
        private void CollectItem(BaseCollectableItem item)
        {
            item?.HandleOnCollect(this);
        }

        #endregion
    }
}
