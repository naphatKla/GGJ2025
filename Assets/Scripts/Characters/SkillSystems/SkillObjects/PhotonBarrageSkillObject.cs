using System;
using UnityEngine;

namespace Characters.SkillSystems.SkillObjects
{
    public class PhotonBarrageSkillObject : BaseSkillObject
    {
        protected override void Awake()
        {
            base.Awake();
            DamageOnTouch.OnHit += o => NotifyDamageDealt();
        }

        /// <summary>
        /// Raised when DamageOnTouch deals damage to a target.
        /// The runtime subscribes to this in order to deactivate the missile on hit.
        /// Wire this up via DamageOnTouch's onDamageDealt callback in the prefab or in code.
        /// </summary>
        public event Action<PhotonBarrageSkillObject> OnHitEnemy;

        /// <summary>
        /// Call from DamageOnTouch's damage callback to notify the runtime.
        /// </summary>
        public void NotifyDamageDealt()
        {
            OnHitEnemy?.Invoke(this);
        }

        public void ClearCallbacks() => OnHitEnemy = null;
    }
}