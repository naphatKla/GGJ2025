using System;
using Characters.SkillSystems.SkillRuntimes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class BaseSkillDataSo : ScriptableObject
    {
        [PropertyTooltip("The cooldown duration (in seconds) before the skill can be used again.")]
        [SerializeField] private float cooldown = 1f;
        
        [Title("")] [ShowInInspector] [PropertyOrder(10000)]
        [PropertyTooltip("")]
        [TypeDrawerSettings(BaseType = typeof(BaseSkillRuntime))]
        private Type _skillRuntime;

        /// <summary>
        /// Gets the cooldown duration (in seconds) for this skill.
        /// </summary>
        public float Cooldown => cooldown;

        /// <summary>
        /// 
        /// </summary>
        public Type SkillRuntime => _skillRuntime;
    }
}