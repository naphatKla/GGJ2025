using Characters.SO.CollectableItemDataSO;

namespace Characters.CollectItemSystems.CollectableItems
{
    public class OrbHealItem : BaseCollectableItem<OrbHealItemDataSo>
    {
        protected override void OnCollect(CollectItemSystem ownerSystem)
        {
            ownerSystem.Owner.HealthSystem.Heal(itemData.HealAmount);
        }
    }
}
