using Characters.CollectItemSystems.CollectableItems;
using GlobalSettings;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.CollectItemSystems
{
    /// <summary>
    /// Handles automatic detection and collection of nearby items within a defined radius.
    /// Uses Unity Physics2D to detect items on the defined collectible layer and pulls them toward the player.
    /// When close enough, the item is collected through its assigned collect logic.
    /// </summary>
    public class CollectItemSystem : MonoBehaviour
    {
        #region Inspector & Variables

        /// <summary>
        /// The radius within which items will be detected and pulled toward the player.
        /// </summary>
        [PropertyTooltip("The radius around the player in which collectible items will be detected and pulled.")]
        [SerializeField] private float pullItemRadius;

        /// <summary>
        /// Whether to show the detection radius as a Gizmo in the editor.
        /// </summary>
        [PropertyTooltip("Show the pull radius as a gizmo in the Scene view for visualization.")]
        [SerializeField] private bool enableGizmos;

        /// <summary>
        /// The LayerMask used to detect which objects are considered collectible.
        /// This value is pulled from global character settings.
        /// </summary>
        private LayerMask collectLayer => CharacterGlobalSettings.Instance.CollectableItemLayerMask;

        #endregion

        #region Unity Methods

        /// <summary>
        /// Runs every physics frame to check for collectible items within the pull radius.
        /// Pulls items toward this GameObject and attempts to collect them once close enough.
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
        /// Draws a visual representation of the item pull radius in the Unity Editor.
        /// Useful for debugging and visualizing item collection zones.
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
