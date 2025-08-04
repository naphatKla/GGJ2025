using UnityEngine;

namespace Characters.SO.StatusEffectSO
{
    [CreateAssetMenu(fileName = "DamageOnTouchEffectData", menuName = "GameData/StatusEffectData/DamageOnTouchEffectData")]
    public class DamageOnTouchEffectDataSo : BaseStatusEffectDataSo
    {
        [SerializeField]
        private float hitPerSec = 1;

        public float HitPerSec => hitPerSec;
    }
}
