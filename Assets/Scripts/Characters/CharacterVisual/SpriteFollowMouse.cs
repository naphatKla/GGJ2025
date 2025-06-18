using System.Collections;
using System.Collections.Generic;
using Characters.MovementSystems;
using Unity.VisualScripting;
using UnityEngine;

public class SpriteFollowMouse : MonoBehaviour
{
    [SerializeField] private RigidbodyMovementSystem ownerMovementSystem;
    [SerializeField] private GameObject _sprite;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _sprite.transform.up = ownerMovementSystem.CurrentVelocity.normalized;
    }
}
