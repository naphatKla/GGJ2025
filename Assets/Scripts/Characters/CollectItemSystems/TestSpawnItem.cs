using System.Collections.Generic;
using Characters.CollectItemSystems.CollectableItems;
using Characters.SO.CollectableItemDataSO;
using UnityEngine;

public class TestSpawnItem : MonoBehaviour
{
    public List<BaseCollectableItemDataSo> itemDatas;
    
    void Start()
    {
        foreach (var itemData in itemDatas)
        {
            BaseCollectableItem item = (BaseCollectableItem) new GameObject().AddComponent(itemData.ItemType);
            item.AssignItemData(itemData);
            item.transform.position = transform.position;
        }
    }
}
