using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCaution : MonoBehaviour
{
    [SerializeField] public float innerRadius;
    private GameObject _target;
    public Vector3 CautionFromTarget()
    {
        Vector3 directionAwayFromPlayer = (transform.position - _target.transform.position).normalized;
        Vector3 escapeDestination = transform.position + directionAwayFromPlayer * innerRadius;
        return escapeDestination;
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, innerRadius);
    }
}
