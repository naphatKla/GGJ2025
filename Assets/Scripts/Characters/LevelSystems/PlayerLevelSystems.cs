using System.Collections.Generic;
using Characters.MovementSystems;
using Characters.StatusEffectSystems;
using Manager;
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
        [SerializeField] protected List<StatusEffectDataPayload> selfStatusEffectsOnLevelUp;
        [SerializeField] protected List<StatusEffectDataPayload> targetStatusEffectOnExplosion;
        
        
        protected override void UpdateExpToLevelUp()
        {
            base.UpdateExpToLevelUp();
            
            if (Level == 1) return;
            StatusEffectManager.ApplyEffectTo(gameObject, selfStatusEffectsOnLevelUp);
            
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

                StatusEffectManager.ApplyEffectTo(target.gameObject, targetStatusEffectOnExplosion);
                
                target.GetComponent<BaseMovementSystem>()
                    .TryMoveToPositionOverTime(knockBackDestination, explosionKnocbackDuration);
            }
        }
    }
}
