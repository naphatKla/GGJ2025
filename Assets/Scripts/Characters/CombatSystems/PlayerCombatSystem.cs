using System;
using Characters.Controllers;
using Characters.FeedbackSystems;
using Characters.HeathSystems;
using UnityEngine;

namespace Characters.CombatSystems
{
    public class PlayerCombatSystem : CombatSystem
    {
        public int TotalCounterDashCount { get; private set; }
        
        public void OnCounterAttackHandler(HealthSystem.HitInfo hitInfo)
        {
            owner.TryPlayFeedback(FeedbackName.Character.CounterAttack);
            PlayerController player = owner as PlayerController;
            player.CombatRankSystem.OnCounterDashCondition(Mathf.CeilToInt(hitInfo.damage));
            TotalCounterDashCount++;
        }
    }
}
