using System;
using System.Collections.Generic;
using Characters.SkillSystems.SkillRuntimes;
using Characters.StatusEffectSystems;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    public abstract class BaseSkillDataSo : SerializedScriptableObject
    {
        [MinValue(1)]
        [SerializeField, PropertyTooltip("The level of this skill.")]
        private int level = 1;

        [Unit(Units.Second)]
        [SerializeField, PropertyTooltip("Cooldown duration (in seconds) before the skill can be used again after activation.")]
        private float cooldown = 1f;

        [Title("Life Time")]
        [SerializeField, PropertyTooltip("If enabled, the skill will automatically exit after a set time.")]
        private bool hasLifeTime;

        [EnableIf(nameof(hasLifeTime))]
        [Unit(Units.Second)]
        [SerializeField, PropertyTooltip("Total active time (in seconds) before the skill automatically exits.")]
        private float lifeTime;

        [Title("Status Effects")]
        [SerializeField, PropertyTooltip("Status effects that will be applied to the user or others when this skill starts.")]
        private List<StatusEffectDataPayload> statusEffectOnSkillStart;

        [Title("Runtime Binding")]
        [ShowInInspector, OdinSerialize, PropertyOrder(10000)]
        [TypeDrawerSettings(BaseType = typeof(BaseSkillRuntime<>))]
        private Type _skillRuntime;

        [Title("Upgrade Chain")]
        [ValidateInput(nameof(IsNextSkillValidate), "Next skill's level must be greater than this skill's level, and must chain back to this node.")]
        [SerializeField, PropertyTooltip("The next skill's data when this skill is upgraded")]
        private BaseSkillDataSo nextSkillDataUpgrade;

        [ValidateInput(nameof(IsPreviousSkillValidate), "Previous skill's level must be less than this skill's level, and must chain forward to this node.")]
        [SerializeField, PropertyTooltip("The previous/base skill node (null = this is base/LV1)")]
        private BaseSkillDataSo previousSkillData;

        // ---- Validation Methods ----

        private bool IsNextSkillValidate()
        {
            if (nextSkillDataUpgrade == null)
                return true;
            // Next ต้อง lv > node ปัจจุบัน และ next.previous == this
            return nextSkillDataUpgrade.level > level
                && nextSkillDataUpgrade.previousSkillData == this;
        }

        private bool IsPreviousSkillValidate()
        {
            if (previousSkillData == null)
                return true;
            // Previous ต้อง lv < node ปัจจุบัน และ previous.next == this
            return previousSkillData.level < level
                && previousSkillData.nextSkillDataUpgrade == this;
        }

        // ---- Properties ----
        public float Cooldown => cooldown;
        public bool HasLifeTime => hasLifeTime;
        public float LifeTime => lifeTime;
        public List<StatusEffectDataPayload> StatusEffectOnSkillStart => statusEffectOnSkillStart;
        public Type SkillRuntime => _skillRuntime;
        public int Level => level;
        public BaseSkillDataSo NextSkillDataUpgrade => nextSkillDataUpgrade;
        public BaseSkillDataSo PreviousSkillData => previousSkillData;

        /// <summary>
        /// Return the root (LV1) node of this skill branch.
        /// </summary>
        public BaseSkillDataSo RootNode
        {
            get
            {
                BaseSkillDataSo root = this;
                while (root.previousSkillData != null)
                    root = root.previousSkillData;
                return root;
            }
        }
    }
}
