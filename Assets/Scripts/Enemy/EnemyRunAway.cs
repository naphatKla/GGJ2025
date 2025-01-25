using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyRunAway : MonoBehaviour
{
    [SerializeField] public float runDistance;

    public Vector3 RunFromTarget(GameObject _target)
    {
        Vector3 directionAwayFromPlayer = (transform.position - _target.transform.position).normalized;
        Vector3 escapeDestination = transform.position + directionAwayFromPlayer * runDistance;
        return escapeDestination;
    }
}
