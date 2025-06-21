using Characters.MovementSystems;
using UnityEngine;

public class SpriteFollowMouse : MonoBehaviour
{
    [SerializeField] private RigidbodyMovementSystem ownerMovementSystem;
    [SerializeField] private GameObject _sprite;

    // Update is called once per frame
    void Update()
    {
        _sprite.transform.up = ownerMovementSystem.CurrentVelocity.normalized;
    }
}
