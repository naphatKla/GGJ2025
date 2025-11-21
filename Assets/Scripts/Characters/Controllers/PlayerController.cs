using System;
using System.Collections.Generic;
using System.Linq;
using Cameras;
using Characters.CollectItemSystems;
using Characters.CombatSystems;
using Characters.ComboSystems;
using Characters.Data;
using Characters.HeathSystems;
using Characters.LevelSystems;
using Characters.ScoreSystems;
using Characters.SkillSystems;
using Characters.SO.CharacterDataSO;
using Characters.UIDisplay;
using UI;
using UnityEngine;

namespace Characters.Controllers
{
    /// <summary>
    /// The player-specific implementation of <see cref="BaseController"/>.
    /// Handles player-related logic such as singleton access to the current player instance.
    /// </summary>
    public class PlayerController : BaseController
    {
        #region Inspector & Variables

        [SerializeField] private CollectItemSystem collectItemSystem;
        [SerializeField] protected CombatRankSystem combatRankSystem;
        [SerializeField] protected FlowStateController flowStateController;
        [SerializeField] protected LevelSystem levelSystem;
        [SerializeField] protected SkillUpgradeController skillUpgradeController;
        [SerializeField] protected ScoreSystem scoreSystem;
        [SerializeField] protected PlayerDisplay playerDisplay;
        
        public CollectItemSystem CollectItemSystem => collectItemSystem;
        public CombatRankSystem CombatRankSystem => combatRankSystem;
        public FlowStateController FlowStateController => flowStateController;
        public LevelSystem LevelSystem => levelSystem;
        public SkillUpgradeController SkillUpgradeController => skillUpgradeController;
        public ScoreSystem ScoreSystem => scoreSystem;
        public PlayerDisplay PlayerDisplay => playerDisplay;

        /// <summary>
        /// A global static reference to the current player instance.
        /// Allows other systems to easily access the active player in the scene.
        /// </summary>
        public static PlayerController Instance { get; private set; }

        #endregion

        #region Unity Methods

        /// <summary>
        /// Called when the script instance is being loaded.
        /// Ensures that only one instance of PlayerController exists in the scene.
        /// If another exists, it is destroyed.
        /// </summary>
        private void Awake()
        {
            if (Instance) return;
            Instance = this;
        }

        public override void AssignCharacterData(BaseCharacterDataSo data)
        {
            if (data is PlayerDataSo playerData)
            {
                collectItemSystem.AssignData(this, playerData.PullItemRadius);
                skillUpgradeController.AssignData(skillSystem, playerData);
                levelSystem.AssignData(this, playerData.BaseExpLevelUp, playerData.StepThreshold, playerData.StepValue);
                scoreSystem.AssignData(this);
                combatRankSystem.AssignRankData(playerData);
                flowStateController.AssignData(gameObject, playerData);
            }
            else
            {
                throw new FormatException();
            }
            
            base.AssignCharacterData(data);
        }

        protected override void SubscribeDependency()
        {
            // counter dash
            PlayerCombatSystem playerCombatSystem = combatSystem as PlayerCombatSystem;
            DamageOnTouch.OnHitWithDamageOnTouch += playerCombatSystem.OnCounterAttackHandler;
            
            // add score on enemy kill
            combatSystem.OnKill += scoreSystem.OnKill;
            combatRankSystem.OnRankChanged += scoreSystem.OnRankModify;
            
            // skill upgrade
            levelSystem.OnLevelUp += skillUpgradeController.OnLevelUp;
            
            // combat rank dependencies
            CombatSystem.OnKill += combatRankSystem.OnKillCondition;
            HealthSystem.OnTakeDamage += combatRankSystem.OnTakeDamageCondition;
            HealthSystem.OnHeal += combatRankSystem.OnHealCondition;
            
            // flow state
            combatRankSystem.OnRankConditionTrigger += flowStateController.OnRankConditionTrigger;

            UIManager.Instance.OnAnyPanelOpen += OnAnyUIOpen;
            UIManager.Instance.OnAllPanelClosed += OnAllUIClosed;
            
            base.SubscribeDependency();
        }

        protected override void UnSubscribeDependency()
        {
            // counter dash
            PlayerCombatSystem playerCombatSystem = combatSystem as PlayerCombatSystem;
            DamageOnTouch.OnHitWithDamageOnTouch -= playerCombatSystem.OnCounterAttackHandler;
            
            // add score on enemy kill
            combatSystem.OnKill -= scoreSystem.OnKill;
            combatRankSystem.OnRankChanged -= scoreSystem.OnRankModify;
            
            // skill upgrade
            levelSystem.OnLevelUp -= skillUpgradeController.OnLevelUp;
            
            // combat rank dependencies
            CombatSystem.OnKill -= combatRankSystem.OnKillCondition;
            HealthSystem.OnTakeDamage -= combatRankSystem.OnTakeDamageCondition;
            HealthSystem.OnHeal -= combatRankSystem.OnHealCondition;
            
            // flow state
            combatRankSystem.OnRankConditionTrigger -= flowStateController.OnRankConditionTrigger;
            
            if (UIManager.IsAlive)
            {
                UIManager.Instance.OnAnyPanelOpen -= OnAnyUIOpen;
                UIManager.Instance.OnAllPanelClosed -= OnAllUIClosed;
            }
            
            base.UnSubscribeDependency();
        }

        public override void ResetAllDependentBehavior()
        {
            levelSystem.ResetLevel();
            skillUpgradeController.ResetSkillUpgradeController();
            combatRankSystem.ResetCombatRankSystem();
            
            if (Cinemachine2DCameraController.Current)
                Cinemachine2DCameraController.Current.ResetAndClearAllRequests();
            
            base.ResetAllDependentBehavior();
        }

        public override void CancelBehaviorOnDead()
        {
            base.CancelBehaviorOnDead();
            levelSystem.Active = false;
        }

        public void OnAnyUIOpen()
        {
            InputSystem.Enable = false;
        }

        public void OnAllUIClosed()
        {
            InputSystem.Enable = true;
        }

        public PlayerSummaryStats GetSummaryStatsOnStateEnd()
        {
            PlayerSummaryStats statsPerRun = new PlayerSummaryStats();
            statsPerRun.totalScore = scoreSystem.CurrentScore;
            statsPerRun.currentLevel = levelSystem.Level;

            statsPerRun.currentRank = combatRankSystem.CurrentRankId;
            statsPerRun.highestRank = combatRankSystem.HighestRecordedRankId;

            var killDict = combatSystem.TotalKillDictionary ?? new Dictionary<string, int>();

            statsPerRun.totalKillDictionary = new Dictionary<string, int>(killDict);
            statsPerRun.totalEnemiesEliminated = killDict.Sum(e => e.Value);
            statsPerRun.totalDamageDeal = combatSystem.TotalDamageDeal;
            statsPerRun.criticalCount = combatSystem.TotalCriticalCount;
            
            PlayerCombatSystem pc = combatSystem as PlayerCombatSystem;
            PlayerHealthSystem hs = HealthSystem as PlayerHealthSystem;
                
            statsPerRun.totalCounterDashCount = pc.TotalCounterDashCount;

            statsPerRun.totalPrimarySkillUsed = skillSystem.TotalPrimarySkillUsed;
            statsPerRun.totalSecondarySkillUsed = skillSystem.TotalSecondarySkillUsed;
            statsPerRun.totalAutoSkillUsed = skillSystem.TotalAutoSkillUsed;

            statsPerRun.totalDamageTaken = hs.TotalDamageTaken;
            statsPerRun.totalHeal = hs.TotalHeal;
            var takeDamageDict = hs.TakeDamageAmountDictionary ?? new();
            statsPerRun.takeDamageAmountDictionary = new Dictionary<string, int>(takeDamageDict);

            var diedDict = hs.DiedAmountDictionary ?? new();
            statsPerRun.diedDictionary = new Dictionary<string, int>(diedDict);
            
            return statsPerRun;
        }

        #endregion
    }
}