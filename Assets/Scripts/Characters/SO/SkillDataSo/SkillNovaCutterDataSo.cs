using Characters.SkillSystems.SkillObjects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    [CreateAssetMenu(fileName = "SkillNovaCutterData", menuName = "GameData/SkillData/SkillNovaCutterData")]
    public class SkillNovaCutterDataSo : BaseSkillDataSo
    {
        [SerializeField] private NovaCutterSkillObject cutterObj;
        [SerializeField] private Vector2 cutterOffset;
        [SerializeField] private float duration = 3f;
        
        [FoldoutGroup("Damage Configs")]
        [PropertyTooltip("Total number of skill objects hit per second")]
        [SerializeField]
        private float damageHitPerSec = 3;
        
        [FoldoutGroup("Damage Configs")]
        [PropertyTooltip("Total number of skill objects hit per second")]
        [SerializeField]
        private float baseDamagePerHit = 5;
        
        [FoldoutGroup("Damage Configs")]
        [PropertyTooltip("Total number of skill objects hit per second")]
        [SerializeField]  [Unit(Units.Percent)]
        private float damageMultiplier = 100;
        
        public NovaCutterSkillObject CutterObj => cutterObj;
        public Vector2 CutterOffset => cutterOffset;
        public float Duration => duration;

        public float DamageHitPerSec => damageHitPerSec;

        public float BaseDamagePerHit => baseDamagePerHit;
        public float DamageMultiplier => damageMultiplier;
    }
}
