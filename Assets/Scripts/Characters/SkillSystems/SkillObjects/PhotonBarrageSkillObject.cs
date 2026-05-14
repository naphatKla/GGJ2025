using System;
using UnityEngine;

namespace Characters.SkillSystems.SkillObjects
{
    public class PhotonBarrageSkillObject : BaseSkillObject
    {
        [SerializeField] private TrailRenderer trail;

        protected override void Awake()
        {
            base.Awake();
            DamageOnTouch.OnHit += o => NotifyDamageDealt();
        }

        public event Action<PhotonBarrageSkillObject> OnHitEnemy;

        public void NotifyDamageDealt()
        {
            OnHitEnemy?.Invoke(this);
        }

        public void ClearCallbacks() => OnHitEnemy = null;

        /// <summary>
        /// Call this AFTER setting the new position but BEFORE SetActive(true).
        /// Clears old trail points so it doesn't stretch from the previous position.
        /// </summary>
        public void ResetTrail()
        {
            if (!trail) return;
            trail.Clear();
        }

        private void OnEnable()
        {
            // Safety net: clear again on enable in case ResetTrail wasn't called
            if (trail) trail.Clear();
        }
    }
}