using System.Collections.Generic;
using Characters.StatusEffectSystems;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    
    [CreateAssetMenu(fileName = "SkillParryData", menuName = "GameData/SkillData/SkillParryData")]
    public class SkillParryDataSo : BaseSkillDataSo
    {
        [SerializeField] private bool stopWhileParry;
        [SerializeField] private float parryDuration;

        [Title("Parry Succession")] [SerializeField] private float explosionDamageMultiplier;
        [SerializeField] private float explosionRadius;
        [SerializeField] private List<StatusEffectDataPayload> explosionEffects;
        [SerializeField] private float knockBackDistance;
        [SerializeField] private float knockBackDuration;

        public bool StopWhileParry => stopWhileParry;
        public float ParryDuration => parryDuration;
        public float ExplosionDamageMultiplier => explosionDamageMultiplier;
        public float ExplosionRadius => explosionRadius;
        public List<StatusEffectDataPayload> ExplosionEffects => explosionEffects;
        public float KnockBackDistance => knockBackDistance;
        public float KnockBackDuration => knockBackDuration;
    }
}
