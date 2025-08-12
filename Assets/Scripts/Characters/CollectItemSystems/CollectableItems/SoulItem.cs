using System;
using Characters.Controllers;
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
            if (ownerSystem.Owner is PlayerController player)
            {
                player.ScoreSystem.AddScore(itemData.Score);
                player.LevelSystem.AddExp(itemData.Exp);
                return;
            }

            throw new NotImplementedException();
        }
    }
}
