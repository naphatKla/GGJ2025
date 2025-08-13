using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.StatusEffectSO
{
    [CreateAssetMenu(fileName = "FlowStageEffectData", menuName = "GameData/StatusEffectData/FlowStageEffectData")]
    public class FlowStageEffectDataSo : BaseStatusEffectDataSo
    {
        [SerializeField] private float damageIncrease;

        [Unit(Units.Percent)] 
        [SerializeField] private float damagePercentIncrease = 0;

        public float DamageIncrease => damageIncrease;
        public float DamagePercentIncrease => damagePercentIncrease;
    }
}
