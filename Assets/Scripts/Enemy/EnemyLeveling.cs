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
    private GameObject _farthestExp = null;

    public Vector3 FindNearestExpOrb()
    {
        _allObjects = GameObject.FindGameObjectsWithTag("Exp");
        float closestDistance = Mathf.Infinity;
        _nearestExp = null;

        foreach (GameObject exp in _allObjects)
        {
            if (Physics2D.OverlapPoint(exp.transform.position, LayerMask.GetMask("Player")))
            {
                continue;
            }

            float distance = Vector2.Distance(this.transform.position, exp.transform.position);

            if (distance <= expdetectDistance && distance < closestDistance)
            {
                closestDistance = distance;
                _nearestExp = exp;
            }
        }

        if (_nearestExp != null)
        {
            return _nearestExp.transform.position;
        }

        return Vector3.zero;
    }
    
    public Vector3 FindFarthestExpOrb()
    {
        _allObjects = GameObject.FindGameObjectsWithTag("Exp");
        float farthestDistance = 0f;
        _farthestExp = null;

        foreach (GameObject exp in _allObjects)
        {
            if (Physics2D.OverlapPoint(exp.transform.position, LayerMask.GetMask("Player")))
            {
                continue;
            }

            float distance = Vector2.Distance(this.transform.position, exp.transform.position);
            
            if (distance <= expdetectDistance && distance > farthestDistance)
            {
                farthestDistance = distance;
                _farthestExp = exp;
            }
        }

        if (_farthestExp != null)
        {
            return _farthestExp.transform.position;
        }

        return Vector3.zero;
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, expdetectDistance);
    }
}
