using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo.TwinOnly
{
    [CreateAssetMenu(fileName = "SkillTwirlData", menuName = "GameData/SkillData/TwinOnly/SkillTwirlData")]
    public class SkillTwirlDataSo : BaseSkillDataSo
    {
        [FoldoutGroup("Damage Configs"), SerializeField]
        private float baseDamagePerHit = 35f;
        
        [Unit(Units.Percent)]
        [FoldoutGroup("Damage Configs"), SerializeField]
        private float damageMultiplier = 100;
        
        [Unit(Units.Second)]
        [FoldoutGroup("Twirl Config"), SerializeField]
        private float twirlDuration = 4;

        [FoldoutGroup("Twirl Config"), SerializeField]
        private AnimationCurve twirlSpeedCurve;

        public float BaseDamagePerHit => baseDamagePerHit;
        public float DamageMultiplier => damageMultiplier;
        public float TwirlDuration => twirlDuration;
        public AnimationCurve TwirlSpeedCurve => twirlSpeedCurve;
    }
}
