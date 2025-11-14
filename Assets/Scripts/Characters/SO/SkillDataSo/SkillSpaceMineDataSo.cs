using Characters.SkillSystems.SkillObjects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    [CreateAssetMenu(fileName = "SkillSpaceMineData", menuName = "GameData/SkillData/SkillSpaceMineData")]
    public class SkillSpaceMineDataSo : BaseSkillDataSo
    {
        [FoldoutGroup("Skill Object")]
        [LabelText("Skill Object Prefab")]
        [PropertyTooltip("Prefab of skill object instances.")]
        [SerializeField] [Required]
        private SpaceMineSkillObject spaceMineSkillObject;
        
        [FoldoutGroup("Damage Configs")]
        [SerializeField] private float baseDamage;
        
        [Unit(Units.Percent)] [FoldoutGroup("Damage Configs")]
        [SerializeField] private float damageMultiplier = 100;

        public SpaceMineSkillObject SpaceMineSkillObject => spaceMineSkillObject;
        public float BaseDamage => baseDamage;
        public float DamageMultiplier => damageMultiplier;
    }
}
