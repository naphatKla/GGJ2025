using UnityEngine;

namespace Characters.SO.CharacterDataSO.EnemyStateDataSO
{
    [CreateAssetMenu(fileName = "SummonerKeepDistanceStateData",
        menuName = "GameData/CharacterData/EnemyStateData/SummonerKeepDistanceStateData")]
    public class SummonerKeepDistanceStateDataSo : BaseEnemyStateDataSo
    {
        [SerializeField] private float detectDistance = 10f;
        [SerializeField] private float preferredMinDistance = 7f;
        [SerializeField] private float preferredMaxDistance = 10f;
        [SerializeField] private float fleeDistance = 5f;
        [SerializeField] private bool repositionWhenTooFar = true;
        [SerializeField] private bool strafeWhileInRange;
        [SerializeField] private int strafeDirection = 1;

        public float DetectDistance => detectDistance;
        public float PreferredMinDistance => preferredMinDistance;
        public float PreferredMaxDistance => preferredMaxDistance;
        public float FleeDistance => fleeDistance;
        public bool RepositionWhenTooFar => repositionWhenTooFar;
        public bool StrafeWhileInRange => strafeWhileInRange;
        public int StrafeDirection => strafeDirection >= 0 ? 1 : -1;

        private void OnValidate()
        {
            fleeDistance = Mathf.Max(0f, fleeDistance);
            preferredMinDistance = Mathf.Max(fleeDistance, preferredMinDistance);
            preferredMaxDistance = Mathf.Max(preferredMinDistance, preferredMaxDistance);
            detectDistance = Mathf.Max(preferredMaxDistance, detectDistance);
        }
    }
}
