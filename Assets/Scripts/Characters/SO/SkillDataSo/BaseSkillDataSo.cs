using System;
using Characters.SkillSystems.SkillRuntimes;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    /// <summary>
    /// Abstract base class for all ScriptableObjects used to define static skill configuration data.
    /// Includes general properties such as cooldown, lifetime duration, and the corresponding runtime type.
    /// Derived classes should extend this to define specific parameters unique to each skill.
    /// </summary>
    public abstract class BaseSkillDataSo : SerializedScriptableObject
    {
        [Unit(Units.Second)]
        [PropertyTooltip("The cooldown duration (in seconds) before this skill can be used again.")]
        [SerializeField] private float cooldown = 1f;

        [Title("Life Time")]
        [PropertyTooltip("Whether the skill has a limited lifetime after activation. If enabled, the skill will auto-exit after the specified time.")]
        [PropertyOrder(9999)]
        [SerializeField] private bool hasLifeTime;

        [Unit(Units.Second)]
        [PropertyTooltip("The active duration of the skill (in seconds) before it forcefully exits. Only used if 'HasLifeTime' is enabled.")]
        [PropertyOrder(9999)]
        [EnableIf(nameof(hasLifeTime))]
        [SerializeField] private float lifeTime;

        [Title("Runtime Binding")]
        [PropertyTooltip("The MonoBehaviour runtime class responsible for executing this skill during gameplay. Must inherit from BaseSkillRuntime<T>.")]
        [ShowInInspector, OdinSerialize, PropertyOrder(10000)]
        [TypeDrawerSettings(BaseType = typeof(BaseSkillRuntime<>))]
        private Type _skillRuntime;

        /// <summary>
        /// Gets the cooldown duration (in seconds) for this skill.
        /// Used by the skill system to determine when the skill can be reused.
        /// </summary>
        public float Cooldown => cooldown;

        /// <summary>
        /// Gets the runtime class type responsible for executing the skill logic.
        /// This should be a subclass of <see cref="BaseSkillRuntime{T}"/> matching this skill data type.
        /// </summary>
        public Type SkillRuntime => _skillRuntime;

        /// <summary>
        /// Indicates whether this skill has a limited active duration after activation.
        /// If true, the skill system will auto-cancel it after <see cref="LifeTime"/>.
        /// </summary>
        public bool HasLifeTime => hasLifeTime;

        /// <summary>
        /// Gets the maximum lifetime of the skill in seconds before it auto-exits.
        /// Only relevant if <see cref="HasLifeTime"/> is true.
        /// </summary>
        public float LifeTime => lifeTime;
    }
}
