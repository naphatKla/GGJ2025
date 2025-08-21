using System;
using Characters.MovementSystems;
using Manager;
using UnityEngine;

namespace Characters.CharacterVisual
{
    public class RotateSpriteByVelocity : MonoBehaviour, IFixedUpdateable
    {
        [SerializeField] private TransformMovementSystem ownerMovementSystem;
        [SerializeField] private GameObject _sprite;

        private void OnEnable()
        {
            FixedUpdateManager.Instance.Register(this);
        }

        private void OnDisable()
        {
            if (!FixedUpdateManager.IsAlive) return;
            FixedUpdateManager.Instance.Unregister(this);
        }
        
        public void OnFixedUpdate()
        {
            if (ownerMovementSystem.CurrentVelocity.magnitude < 0.01f) return;
            _sprite.transform.up = ownerMovementSystem.CurrentVelocity.normalized;
        }
    }
}
