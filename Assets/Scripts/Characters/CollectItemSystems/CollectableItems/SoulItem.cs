using System;
using Characters.SO.CollectableItemDataSO;
using GameControl;
using UnityEngine;

namespace Characters.CollectItemSystems.CollectableItems
{
    /// <summary>
    /// 
    /// </summary>
    public class SoulItem : BaseCollectableItem<SoulItemDataSo>
    {
        [SerializeField] private PoolableComponent poolable;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ownerSystem"></param>
        protected override void OnCollect(CollectItemSystem ownerSystem)
        {
            PoolingSystem.Instance.Despawn(poolable.ComponenetId, gameObject);
        }
    }
}
