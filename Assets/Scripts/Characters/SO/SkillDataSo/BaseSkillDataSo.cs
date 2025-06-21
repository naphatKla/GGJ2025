using System;
using System.Collections.Generic;
using Characters.SkillSystems.SkillRuntimes;
using Characters.StatusEffectSystems;
using Manager;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Serialization;

namespace Characters.SO.SkillDataSo
{
    /// <summary>
    /// Abstract base class for all ScriptableObjects used to define static skill configuration data.
    /// Includes cooldown control, optional lifetime behavior, feedback events, status effect triggers, 
    /// and runtime MonoBehaviour binding. Subclasses should extend with their own configuration fields.
    /// </summary>
    public abstract class BaseSkillDataSo : SerializedScriptableObject
    {
        #region Inspector & Variables
        
        // ---------------- Cooldown ----------------

        [Unit(Units.Second)]
        [PropertyTooltip("Cooldown duration (in seconds) before the skill can be used again after activation.")]
        [SerializeField] 
        private float cooldown = 1f;

        // ---------------- Life Time ----------------

        [Title("Life Time")]

        [PropertyTooltip("If enabled, the skill will automatically exit after a set time.")]
        [PropertyOrder(9999)]
        [SerializeField] 
        private bool hasLifeTime;

        [EnableIf(nameof(hasLifeTime))] [Unit(Units.Second)]
        [PropertyTooltip("Total active time (in seconds) before the skill automatically exits. Used only if 'Has Life Time' is enabled.")]
        [PropertyOrder(9999)]
        [SerializeField] 
        private float lifeTime;
        
        // ---------------- Status Effects ----------------
        [Title("Status Effects")]
        
        [PropertyTooltip("Status effects that will be applied to the user or others when this skill starts.")]
        [PropertyOrder(9999)]
        [SerializeField] 
        private List<StatusEffectDataPayload> statusEffectOnSkillStart;

        // ---------------- Runtime Binding ----------------

        [Title("Runtime Binding")]
        
        [PropertyTooltip("The MonoBehaviour runtime class that will execute this skill. Must inherit from BaseSkillRuntime<T>.")]
        [ShowInInspector, OdinSerialize, PropertyOrder(10000)]
        [TypeDrawerSettings(BaseType = typeof(BaseSkillRuntime<>))]
        private Type _skillRuntime;

        // ---------------- Properties ----------------

        /// <summary>
        /// Cooldown duration (in seconds) before the skill can be re-used.
        /// </summary>
        public float Cooldown => cooldown;

        /// <summary>
        /// Whether this skill automatically ends after a set lifetime.
        /// </summary>
        public bool HasLifeTime => hasLifeTime;

        /// <summary>
        /// Maximum active time (in seconds) before the skill auto-cancels.
        /// Only used if <see cref="HasLifeTime"/> is true.
        /// </summary>
        public float LifeTime => lifeTime;

        /// <summary>
        /// List of status effects to apply when this skill starts.
        /// Can target the skill user, allies, or enemies depending on implementation.
        /// </summary>
        public List<StatusEffectDataPayload> StatusEffectOnSkillStart => statusEffectOnSkillStart;


        /// <summary>
        /// The runtime MonoBehaviour class that handles this skill’s logic. 
        /// Must inherit from <see cref="BaseSkillRuntime{T}"/>.
        /// </summary>
        public Type SkillRuntime => _skillRuntime;
        
        #endregion
    }
}
