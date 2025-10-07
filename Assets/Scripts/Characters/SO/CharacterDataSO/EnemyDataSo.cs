using System.Collections.Generic;
using Characters.SO.CharacterDataSO.EnemyStateDataSO;
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
        private float delayBeforePerformSkill = 0.5f;

        [FoldoutGroup("State Machine Controller")] [SerializeField]
        private BaseEnemyStateDataSo defaultState;
        
        [FoldoutGroup("State Machine Controller")] [SerializeField]
        private List<EnemyStateDataPayload> stateList;
        
        public int ExpDrop => expDrop;
        public float StopDistance => stopDistance;
        public float PerformSkillDistance => performSkillDistance;
        public float DelayBeforePerformSkill => delayBeforePerformSkill;
        public BaseEnemyStateDataSo DefaultState => defaultState;
        public List<EnemyStateDataPayload> StateList => stateList;
    }
}