using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    [CreateAssetMenu(fileName = "SkillSpaceMineData", menuName = "GameData/SkillData/SkillSpaceMineData")]
    public class SkillSpaceMineDataSo : BaseSkillDataSo
    {
        [FoldoutGroup("Damage Configs")]
        [SerializeField] private float baseDamage;
        
        [Unit(Units.Percent)] [FoldoutGroup("Damage Configs")]
        [SerializeField] private float damageMultiplier = 100;
        
        [FoldoutGroup("Skill Configs")] 
        [SerializeField] private float chargeDuration;
        
        [FoldoutGroup("Skill Configs")]
        [SerializeField] private float explosionRadius;
    }
}
