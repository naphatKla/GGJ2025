using System.Collections.Generic;
using Characters.StatusEffectSystems;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo.TwinOnly
{
    [CreateAssetMenu(fileName = "SkillTwirlData", menuName = "GameData/SkillData/TwinOnly/SkillTwirlData")]
    public class SkillTwirlDataSo : BaseSkillDataSo
    {
        // Damage Configs
        [FoldoutGroup("Damage Configs"), SerializeField]
        private float baseDamagePerHit = 5f;

        [Unit(Units.Percent)] [FoldoutGroup("Damage Configs"), SerializeField]
        private float damageMultiplier = 100;

        [FoldoutGroup("Damage Configs"), SerializeField]
        private float damageRadius = 10.15f;

        // Expand Phase
        [FoldoutGroup("Twirl Config/Expand Phase"), SerializeField]
        private float expandRadius = 8;

        [Unit(Units.Second)] [FoldoutGroup("Twirl Config/Expand Phase"), SerializeField]
        private float startExpandDuration = 0.5f;

        // Spin Phase
        [Unit(Units.Second)] [FoldoutGroup("Twirl Config/Spin Phase"), SerializeField]
        private float twirlDuration = 4;

        [FoldoutGroup("Twirl Config/Spin Phase"), SerializeField]
        private int spinRound = 10;

        [Unit(Units.Percent)] [FoldoutGroup("Twirl Config/Spin Phase"), SerializeField]
        private float speedUpMultiplier = 100f;
        
        [Unit(Units.Second)] [FoldoutGroup("Twirl Config/Spin Phase"), SerializeField]
        private float delayEnableDamageAfterStartSpin = 1f;

        [Unit(Units.Second)] [FoldoutGroup("Twirl Config/Spin Phase"), SerializeField]
        private float damageDuration = 2f;

        // Merge back phase
        [Unit(Units.Second)] [FoldoutGroup("Twirl Config/Merge Phase"), SerializeField]
        private float mergeBackDuration = 0.5f;

        [FoldoutGroup("Twirl Config/Merge Phase"), SerializeField]
        private List<StatusEffectDataPayload> effectSelfOnSuccess;
        
        [FoldoutGroup("Twirl Config"), SerializeField]
        private AnimationCurve twirlSpeedCurve;

        public float BaseDamagePerHit => baseDamagePerHit;
        public float DamageMultiplier => damageMultiplier;
        public float DamageRadius => damageRadius;
        public float SpeedUpMultiplier => speedUpMultiplier;
        public float DelayEnableDamageAfterStartSpin => delayEnableDamageAfterStartSpin;
        public float DamageDuration => damageDuration;
        public float ExpandRadius => expandRadius;
        public float StartExpandDuration => startExpandDuration;
        public float MergeBackDuration => mergeBackDuration;
        public float TwirlDuration => twirlDuration;
        public int SpinRound => spinRound;
        public List<StatusEffectDataPayload> EffectSelfOnSuccess => effectSelfOnSuccess;
        public AnimationCurve TwirlSpeedCurve => twirlSpeedCurve;
    }
}