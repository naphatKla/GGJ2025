using UnityEngine;

namespace Characters.SO.CollectableItemDataSO
{
    [CreateAssetMenu(fileName = "HealItemData", menuName = "GameData/CollectableItemData/HealItemData")]
    public class OrbHealItemDataSo : BaseCollectableItemDataSo
    {
        [SerializeField] private float healAmount = 10f;
        public float HealAmount => healAmount;
    }
}
