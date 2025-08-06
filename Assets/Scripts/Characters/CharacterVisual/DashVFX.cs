using Characters.MovementSystems;
using UnityEngine;

namespace Characters.CharacterVisual
{
    public class DashVFX : MonoBehaviour
    {
        [SerializeField] private TransformMovementSystem ownerMovementSystem;
        [SerializeField] private ParticleSystem particleSystem;
        private Vector2 direction;
        
        private void FixedUpdate()
        {
            if (!particleSystem.isPlaying) return;
            transform.up = ownerMovementSystem.CurrentVelocity.normalized;
        }
    }
}
