using System;
using System.Collections.Generic;
using Characters.MovementSystems;
using UnityEngine;

public class TestMovement : MonoBehaviour
{
    [SerializeField] private BaseMovementSystem movementSystem;
    [SerializeField] private List<Transform> moveTothis;
    
    void Start()
    {
      
    }

    private void FixedUpdate()
    {
        movementSystem.TryMoveWithInertia(moveTothis[0].position);
    }
}
