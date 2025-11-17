using Characters.Controllers;
using Characters.FeedbackSystems;
using Characters.HeathSystems;


namespace Characters.CombatSystems
{
    public class PlayerCombatSystem : CombatSystem
    {
        public int TotalCounterDashCount { get; private set; }
        
        public void OnCounterAttackHandler(int damageNegate)
        {
            owner.TryPlayFeedback(FeedbackName.Character.CounterAttack);
            PlayerController player = owner as PlayerController;
            PlayerHealthSystem playerHealthSystem = player.HealthSystem as PlayerHealthSystem;
            player.CombatRankSystem.OnCounterDashCondition(damageNegate);
            playerHealthSystem.OnCounterDash();
            TotalCounterDashCount++;
        }
    }
}
