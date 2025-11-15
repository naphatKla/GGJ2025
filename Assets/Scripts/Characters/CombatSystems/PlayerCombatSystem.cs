using System;
using Characters.FeedbackSystems;

namespace Characters.CombatSystems
{
    public class PlayerCombatSystem : CombatSystem
    {
        public int TotalCounterDashCount { get; private set; }
        
        public void OnCounterAttackHandler()
        {
            owner.TryPlayFeedback(FeedbackName.Character.CounterAttack);
            TotalCounterDashCount++;
        }
    }
}
