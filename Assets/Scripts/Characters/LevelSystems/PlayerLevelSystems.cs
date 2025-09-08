using Characters.MovementSystems;
using UnityEngine;

namespace Characters.LevelSystems
{
    public class PlayerLevelSystems : LevelSystem
    {
        [SerializeField] private bool useExplosionOnLevelUp = true;
        [SerializeField] protected float explosionRadius = 10f;
        [SerializeField] protected float explosionKnockBackDistance = 10f;
        [SerializeField] private float explosionKnocbackDuration = 0.35f;
        [SerializeField] private LayerMask targetLayerMask;
        
        
        protected override void UpdateExpToLevelUp()
        {
            base.UpdateExpToLevelUp();
            
            if (!useExplosionOnLevelUp) return;
            ExplosionOnLevelUp();
        }

        private void ExplosionOnLevelUp()
        {
            var targetsInRange =
                Physics2D.OverlapCircleAll(transform.position, explosionRadius, targetLayerMask);

            foreach (var target in targetsInRange)
            {
                Vector2 knockBackDirection = target.transform.position - transform.position;
                Vector2 knockBackDestination = (Vector2)target.transform.position +
                                               (knockBackDirection.normalized * explosionKnockBackDistance);

                target.GetComponent<BaseMovementSystem>()
                    .TryMoveToPositionOverTime(knockBackDestination, explosionKnocbackDuration);
            }
        }
    }
}
