using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.CollectableItemDataSO
{
    [CreateAssetMenu(fileName = "SoulItemData", menuName = "GameData/CollectableItemData/SoulItemData")]
    public class SoulItemDataSo : BaseCollectableItemDataSo
    {
        //score to add
        [Title("Soul Data")]
        [PropertyTooltip("")]
        [SerializeField] private int score;

        /// <summary>
        /// 
        /// </summary>
        public int Score => score;
    }
}
