using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace Characters.MovementSystem
{
    public class EnemyMovement : BaseMovementSystem
    {
        [Required] [SerializeField] private NavMeshAgent agent;

        private void Start()
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }

        protected override void Move(Vector2 position)
        {
            agent.SetDestination(position);
        }
    }
}

