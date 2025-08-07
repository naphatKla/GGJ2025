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
        [SerializeField] [PreviewField(Alignment = ObjectFieldAlignment.Center, Height = 128)] [HideLabel, Space(10)]
        private Sprite skillIcon;
        
        [Title("Details")]
        [SerializeField] private string skillName;
        
        [Space] [SerializeField] [LabelText("")] [MultiLineProperty]
        private string skillDescription;
        
        [HorizontalGroup("SkillChain")]
        [BoxGroup("SkillChain/<-Previous Skill")]
        [LabelText("")]
        [ValidateInput(nameof(IsPreviousSkillValidate),
            "Previous skill's level must be less than this skill's level, and must chain forward to this node.")]
        [SerializeField, PropertyTooltip("The previous/base skill node (null = this is base/LV1)")]
        private BaseSkillDataSo previousSkillData;

        [HorizontalGroup("SkillChain")]
        [BoxGroup("SkillChain/Next Skill->")]
        [LabelText("")]
        [ValidateInput(nameof(IsNextSkillValidate),
            "Next skill's level must be greater than this skill's level, and must chain back to this node.")]
        [SerializeField, PropertyTooltip("The next skill's data when this skill is upgraded")]
        private BaseSkillDataSo nextSkillDataUpgrade;
        
        [Space] [Title("Configs")]
        [ProgressBar(0, 10, ColorGetter = nameof(GetLevelBarColor), Segmented = true, DrawValueLabel = true, Height = 20)]
        [SerializeField, PropertyTooltip("The level of this skill.")]
        [MinValue(1)]
        private int level = 1;

        [Unit(Units.Second)]
        [SerializeField,
         PropertyTooltip("Cooldown duration (in seconds) before the skill can be used again after activation.")]
        [PropertySpace(SpaceAfter = 10, SpaceBefore = 0)]
        private float cooldown = 1f;
        
        [FoldoutGroup("Status Effects", Order = 100)] 
        [SerializeField,
         PropertyTooltip("Status effects that will be applied to the user or others when this skill starts.")]
        private List<StatusEffectDataPayload> statusEffectOnSkillStart;

        [Space] [Title("Runtime Binding")]
        [ShowInInspector, OdinSerialize, PropertyOrder(10000)]
        [TypeDrawerSettings(BaseType = typeof(BaseSkillRuntime<>))]
        private Type _skillRuntime;

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
        public Sprite SkillIcon => skillIcon;
        public string SkillDescription => skillDescription;
        public float Cooldown => cooldown;
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

        private Color GetLevelBarColor()
        {
            if (level < 4) return Color.green;
            if (level < 7) return Color.yellow;
            return Color.cyan;
        }
    }
}