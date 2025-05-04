using System;
using Characters.SkillSystems.SkillRuntimes;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    /// <summary>
    /// Abstract base class for all ScriptableObjects used to define static skill configuration data.
    /// Includes general properties such as cooldown and the corresponding runtime skill type.
    /// Derived classes should define additional parameters specific to each skill.
    /// </summary>
    public abstract class BaseSkillDataSo : SerializedScriptableObject
    {
        [PropertyTooltip("The cooldown duration (in seconds) before this skill can be used again.")]
        [SerializeField] private float cooldown = 1f;

        [PropertyTooltip("The runtime MonoBehaviour class that will execute this skill logic during gameplay. Must inherit from BaseSkillRuntime<T>.")]
        [Title("Runtime Binding")]
        [ShowInInspector, OdinSerialize, PropertyOrder(10000)]
        [TypeDrawerSettings(BaseType = typeof(BaseSkillRuntime<>))]
        private Type _skillRuntime; 

        /// <summary>
        /// Gets the cooldown duration (in seconds) for this skill.
        /// Used by the skill system to determine when the skill can be reused.
        /// </summary>
        public float Cooldown => cooldown; 

        /// <summary>
        /// Gets the runtime class type responsible for executing the skill.
        /// This should be a subclass of <see cref="BaseSkillRuntime{T}"/> that corresponds to this data asset.
        /// </summary>
        public Type SkillRuntime => _skillRuntime;
    }
}