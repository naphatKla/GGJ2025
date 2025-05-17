using Characters.SO.StatusEffectSO;
using UnityEngine;

namespace Characters.StatusEffectSystem.StatusEffects
{
    /// <summary>
    /// Abstract base class for all runtime status effects.
    /// Contains common properties like name, duration, level, and lifecycle methods.
    /// </summary>
    public abstract class BaseStatusEffect
    {
        #region Inspector & Variables
        
        /// <summary>
        /// The type of status effect represented by an enum value.
        /// </summary>
        protected StatusEffectName effectName;

        /// <summary>
        /// Remaining time before the effect expires.
        /// </summary>
        protected float currentDuration;

        /// <summary>
        /// Priority level of this effect used for overriding logic.
        /// </summary>
        protected int level;

        /// <summary>
        /// Gets the effect's identifier.
        /// </summary>
        public StatusEffectName EffectName => effectName;

        /// <summary>
        /// Gets or sets the remaining duration of the effect.
        /// </summary>
        public float CurrentDuration
        {
            get => currentDuration;
            set => currentDuration = value;
        }

        /// <summary>
        /// Gets the priority level of the effect.
        /// </summary>
        public int Level => level;

        /// <summary>
        /// Gets whether the effect is finished and should be removed.
        /// </summary>
        public bool IsDone => currentDuration <= 0f;
        
        #endregion
        
        #region Methods
        
        /// <summary>
        /// Assigns the effect data and optionally overrides its duration.
        /// Must be called before using the effect.
        /// </summary>
        /// <param name="data">The ScriptableObject containing static effect data.</param>
        /// <param name="duration">Optional duration override. If 0, default is used.</param>
        public abstract void AssignEffectData(BaseStatusEffectDataSo data, float duration = 0);

        /// <summary>
        /// Called when the effect is first applied to the GameObject.
        /// </summary>
        /// <param name="owner">The GameObject receiving the effect.</param>
        public abstract void OnStart(GameObject owner);

        /// <summary>
        /// Called every frame while the effect is active.
        /// </summary>
        /// <param name="deltaTime">Time passed since last frame.</param>
        public abstract void OnUpdate(float deltaTime);

        /// <summary>
        /// Called once when the effect ends or is removed.
        /// </summary>
        public abstract void OnExit();

        /// <summary>
        /// Immediately marks the effect as finished by setting its duration to zero.
        /// </summary>
        public void ClearThisEffect() => currentDuration = 0;
        
        #endregion
    }

    /// <summary>
    /// Generic base class for status effects that use typed effect data.
    /// </summary>
    /// <typeparam name="T">The type of ScriptableObject used to define this effect.</typeparam>
    public abstract class BaseStatusEffect<T> : BaseStatusEffect where T : BaseStatusEffectDataSo
    {
        #region Inspector & Variables
        
        /// <summary>
        /// Strongly-typed reference to the effect's ScriptableObject data.
        /// </summary>
        protected T effectData;
        
        #endregion

        #region Methods
        
        /// <summary>
        /// Assigns the typed effect data and initializes key properties such as name, duration, and level.
        /// </summary>
        /// <param name="data">The ScriptableObject data for the effect.</param>
        /// <param name="duration">Optional override for effect duration.</param>
        public override void AssignEffectData(BaseStatusEffectDataSo data, float duration = 0)
        {
            effectData = data as T;
            effectName = effectData.EffectName;
            currentDuration = duration;
            level = effectData.Level;
        }
        
        #endregion
    }
}
