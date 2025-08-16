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

        [Unit(Units.Percent)]
        [SerializeField] private float speedPercentIncrease = 0;

        [Title("Player Only")]
        [SerializeField] private bool expandCamera;

        [EnableIf(nameof(expandCamera))]
        [SerializeField] private float additionalExpandSize = 1.5f;
        
        public float DamageIncrease => damageIncrease;
        public float DamagePercentIncrease => damagePercentIncrease;
        public float SpeedPercentIncrease => speedPercentIncrease;
        public bool ExpandCamera => expandCamera;
        public float AdditionalExpandSize => additionalExpandSize;
    }
}
