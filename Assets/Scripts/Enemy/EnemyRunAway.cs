using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyRunAway : MonoBehaviour
{
    [SerializeField] public float runDistance;
    private GameObject _target;
    
    private void Start()
    {
        _target = GameObject.FindGameObjectWithTag("Player");
    }
    
    public Vector3 RunFromTarget()
    {
        Vector3 directionAwayFromPlayer = (transform.position - _target.transform.position).normalized;
        Vector3 escapeDestination = transform.position + directionAwayFromPlayer * runDistance;
        return escapeDestination;
    }
}
