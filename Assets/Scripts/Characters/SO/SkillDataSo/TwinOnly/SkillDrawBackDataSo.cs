using System.Collections.Generic;
using Characters.StatusEffectSystems;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo.TwinOnly
{
    [CreateAssetMenu(fileName = "SkillDrawBackData", menuName = "GameData/SkillData/TwinOnly/SkillDrawBackData")]
    public class SkillDrawBackDataSo : BaseSkillDataSo
    {
        [FoldoutGroup("Damage Configs"), SerializeField]
        private float baseDamagePerHit = 5f;

        [Unit(Units.Percent)] [FoldoutGroup("Damage Configs"), SerializeField]
        private float damageMultiplier = 100;

        [Unit(Units.Percent)]
        [FoldoutGroup("DrawBack Config"), SerializeField]
        private float availableOnHpLessOrEqualThan = 50f;
        
        [FoldoutGroup("DrawBack Config"), SerializeField]
        private float chargeTime;
        
        [FoldoutGroup("DrawBack Config"), SerializeField]
        private List<StatusEffectDataPayload> effectSelfOnSuccess;
        
        public float BaseDamagePerHit => baseDamagePerHit;
        public float AvailableOnHpLessOrEqualThan => availableOnHpLessOrEqualThan;
        public float DamageMultiplier => damageMultiplier;
        public float ChargeTime => chargeTime;
        public List<StatusEffectDataPayload> EffectSelfOnSuccess => effectSelfOnSuccess;
    }
}
