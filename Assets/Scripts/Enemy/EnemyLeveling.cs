using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class EnemyLeveling : MonoBehaviour
{
    [SerializeField] private float expdetectDistance;

    private GameObject[] _allObjects;
    private float _distance;
    [ShowInInspector]
    private GameObject _nearestExp;

    public Vector3 FindNearestExpOrb()
    {
        _allObjects = GameObject.FindGameObjectsWithTag("Exp");
        float closestDistance = Mathf.Infinity;
        GameObject closestExp = null;

        foreach (GameObject exp in _allObjects)
        {
            float distance = Vector2.Distance(this.transform.position, exp.transform.position);

            if (distance <= expdetectDistance && distance < closestDistance)
            {
                closestDistance = distance;
                closestExp = exp;
            }
        }

        if (closestExp != null)
        {
            return closestExp.transform.position;
        }

        return Vector3.zero;
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, expdetectDistance);
    }
}
