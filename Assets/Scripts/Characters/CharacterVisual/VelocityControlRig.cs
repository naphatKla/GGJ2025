using System.Collections;
using System.Collections.Generic;
using Characters.MovementSystems;
using UnityEngine;

public class VelocityControlRig : MonoBehaviour
{
    public RigidbodyMovementSystem RbSystem;
    public Animator animator;

    void Update()
    {
        animator.SetFloat("x", RbSystem.CurrentVelocity.x);
        animator.SetFloat("y", RbSystem.CurrentVelocity.y);
    }
}
