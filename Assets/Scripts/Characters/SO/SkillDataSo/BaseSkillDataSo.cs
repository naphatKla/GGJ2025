using System;
using Characters.SkillSystems.SkillRuntimes;
using Manager;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    /// <summary>
    /// Abstract base class for all ScriptableObjects used to define static skill configuration data.
    /// Includes general-purpose properties like cooldown, optional lifetime, feedback triggers, and runtime binding.
    /// Derived skill data classes should inherit from this and add their specific configuration parameters.
    /// </summary>
    public abstract class BaseSkillDataSo : SerializedScriptableObject
    {
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

        [Unit(Units.Second)]
        [PropertyTooltip("Total active time (in seconds) before the skill automatically exits. Used only if 'Has Life Time' is enabled.")]
        [PropertyOrder(9999)]
        [EnableIf(nameof(hasLifeTime))]
        [SerializeField] 
        private float lifeTime;

        [PropertyTooltip("The global feedback to trigger when the skill begins execution (e.g., visual or sound).")]
        [PropertyOrder(9999)]
        [SerializeField] 
        private FeedbackName startFeedback;

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
        /// The runtime MonoBehaviour class that handles this skill’s logic. 
        /// Must inherit from <see cref="BaseSkillRuntime{T}"/>.
        /// </summary>
        public Type SkillRuntime => _skillRuntime;

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
        /// The global feedback identifier to trigger when the skill begins.
        /// </summary>
        public FeedbackName StartFeedback => startFeedback;
    }
}
