using Characters.ComboSystems;
using Characters.LevelSystems;
using Characters.SkillSystems;
using Characters.SO.CharacterDataSO;
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
        
        [SerializeField] public ComboSystem comboSystem;
        [SerializeField] protected LevelSystem levelSystem;
        [SerializeField] protected SkillUpgradeController skillUpgradeController;
        
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
            skillUpgradeController.AssignData(skillSystem, data as PlayerDataSo);
            base.AssignCharacterData(data);
        }
        
        protected override void SubscribeDependency()
        {
            levelSystem.OnLevelUp += skillUpgradeController.OnLevelUp;
            combatSystem.OnDealDamage += comboSystem.RegisterHit;
            
            base.SubscribeDependency();
        }

        protected override void UnSubscribeDependency()
        {
            levelSystem.OnLevelUp -= skillUpgradeController.OnLevelUp;
            combatSystem.OnDealDamage -= comboSystem.RegisterHit;
            
            base.UnSubscribeDependency();
        }

        public override void ResetAllDependentBehavior()
        {
            levelSystem.ResetLevel();
            skillUpgradeController.ResetSkillUpgradeController();
            
            base.ResetAllDependentBehavior();
        }

        #endregion
    }
}