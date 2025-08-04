using System;
using Characters.CollectItemSystems.CollectableItems;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Characters.SO.CollectableItemDataSO
{
    /// <summary>
    /// Abstract base class for collectable item data used via ScriptableObject.
    /// Defines visual presentation and pulling behavior, along with the runtime class type for instantiation.
    /// </summary>
    public abstract class BaseCollectableItemDataSo : SerializedScriptableObject
    {
        #region Inspector & Variables
        
        [Title("Item Preview")]
        
        [PreviewField(Alignment = ObjectFieldAlignment.Center, Height = 128)]
        [HideLabel, Space(10)]
        [PropertyTooltip("Sprite preview shown in the editor for visual reference.")]
        [SerializeField]
        private Sprite icon;

        [Title("Details")]
        [PropertyTooltip("Time (in seconds) before this item becomes collectible after being spawned.")]
        [SerializeField]
        private float pickupDelay = 0.5f;

        [Title("Pull Settings")]
        [PropertyTooltip("Duration (in seconds) it takes for the item to reach the collector. Min - Max the value random between this values")]
        [PropertyOrder(9999)]
        [SerializeField]
        private Vector2 pullDuration = new Vector2(0.25f, 0.4f);

        [PropertyTooltip("AnimationCurve controlling the pull easing over time.")]
        [PropertyOrder(9999)]
        [SerializeField] 
        private AnimationCurve pullEase;

        [PropertyTooltip("Optional movement curve to control lateral or arcing motion.")]
        [PropertyOrder(9999)]
        [SerializeField] 
        private AnimationCurve pullCurve;
        
        /// <summary>
        /// The visual icon displayed for this item.
        /// </summary>
        public Sprite Icon => icon;

        /// <summary>
        /// Time (in seconds) after spawning before this item can be picked up by a character.
        /// Used to prevent immediate collection on spawn.
        /// </summary>
        public float PickupDelay => pickupDelay;
        
        /// <summary>
        /// The time in seconds it takes for the item to reach its target when pulled.
        /// </summary>
        public Vector2 PullDuration => pullDuration;

        /// <summary>
        /// The movement curve used for sideways or arcing motion while pulling.
        /// </summary>
        public AnimationCurve PullCurve => pullCurve;

        /// <summary>
        /// The easing curve that defines acceleration during the pull.
        /// </summary>
        public AnimationCurve PullEase => pullEase;
        
        #endregion
    }
}
