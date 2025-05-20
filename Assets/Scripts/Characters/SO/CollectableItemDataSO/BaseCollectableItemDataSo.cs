using System;
using Characters.CollectItemSystems.CollectableItems;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Characters.SO.CollectableItemDataSO
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class BaseCollectableItemDataSo : SerializedScriptableObject
    {
        #region Inspector & Variables
        
        [Title("Item Preview")]
        [PreviewField(Alignment = ObjectFieldAlignment.Center, Height = 128)]
        [HideLabel, Space(10)]
        [PropertyTooltip(" ")]
        [SerializeField]
        private Sprite icon;
        
        // duration use to move to picker 
        [Title("Pull Settings")]
        
        [PropertyTooltip("")] [PropertyOrder(9999)]
        [SerializeField] private float pullDuration = 0.5f;
        
        // move ease
        [PropertyTooltip("")] [PropertyOrder(9999)]
        [SerializeField] private AnimationCurve pullEase;
        
        // movement curve
        [PropertyTooltip("")] [PropertyOrder(9999)]
        [SerializeField] private AnimationCurve pullCurve;
        
        // ---------------- Runtime Binding ----------------

        [Title("Runtime Binding")]
        
        [PropertyTooltip(" ")]
        [ShowInInspector, OdinSerialize, PropertyOrder(10000)]
        [TypeDrawerSettings(BaseType = typeof(BaseCollectableItem<>))]
        private Type _itemType;

        /// <summary>
        /// 
        /// </summary>
        public Sprite Icon => icon;

        /// <summary>
        /// 
        /// </summary>
        public float PullDuration => pullDuration;
        
        /// <summary>
        /// 
        /// </summary>
        public AnimationCurve PullCurve => pullCurve;

        /// <summary>
        /// 
        /// </summary>
        public AnimationCurve PullEase => pullEase;
        
        /// <summary>
        /// 
        /// </summary>
        public Type ItemType => _itemType;
        
        #endregion
    }
}
