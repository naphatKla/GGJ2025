using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.CharacterDataSO
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "GameData/CharacterData/EnemyData")]
    public class EnemyDataSo : BaseCharacterDataSo
    {
        [FoldoutGroup("Combat")]
        [SerializeField, PropertyTooltip("Exp drop after dead")]
        private int expDrop;

        [FoldoutGroup("Combat/AI")]
        [SerializeField, PropertyTooltip("Stop moving when within this distance from target.")]
        private float stopDistance = 8f;

        [FoldoutGroup("Combat/AI")]
        [SerializeField, PropertyTooltip("Begin skill if within this distance.")]
        private float performSkillDistance = 8f;

        [FoldoutGroup("Combat/AI")]
        [SerializeField, PropertyTooltip("Delay before performing skill after being eligible.")]
        private float skillChargeDelay = 0.5f;

        public int ExpDrop => expDrop;
        public float StopDistance => stopDistance;
        public float PerformSkillDistance => performSkillDistance;
        public float SkillChargeDelay => skillChargeDelay;
    }
}