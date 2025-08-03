using System;
using Characters.LevelSystems;
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ownerSystem"></param>
        protected override void OnCollect(CollectItemSystem ownerSystem)
        {
            if (!ownerSystem.TryGetComponent(out LevelSystem levelSystem)) return;
            levelSystem.AddExp(itemData.Exp);
        }
    }
}
