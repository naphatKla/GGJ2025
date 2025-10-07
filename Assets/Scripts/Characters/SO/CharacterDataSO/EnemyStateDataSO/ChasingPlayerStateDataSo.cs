using UnityEngine;

namespace Characters.SO.CharacterDataSO.EnemyStateDataSO
{
    [CreateAssetMenu(fileName = "ChasingPlayerStateData",
        menuName = "GameData/CharacterData/EnemyStateData/ChasingPlayerStateData")]
    public class ChasingPlayerStateDataSo : BaseEnemyStateDataSo
    {
        [SerializeField] private float stopDistance = 8f;
        [SerializeField] private float performSkillDistance = 8f;

        public float StopDistance => stopDistance;
        public float PerformSkillDistance => performSkillDistance;
    }
}