using Characters.SO.CollectableItemDataSO;
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
            //Debug.Log("Add Score" + itemData.Score);
        }
    }
}
