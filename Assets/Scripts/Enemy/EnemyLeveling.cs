using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class EnemyLeveling : MonoBehaviour
{
    [SerializeField] private float expdetectDistance;
    [SerializeField] public Transform expParent;

    private GameObject[] _allObjects;
    private float _distance;
    [ShowInInspector]
    private GameObject _nearestExp;

    public Vector3 FindNearestExpOrb()
    {
        _allObjects = GameObject.FindGameObjectsWithTag("Exp");
        for (int i = 0; i < expParent.childCount; i++)
        {
            _distance = Vector2.Distance(this.transform.position ,_allObjects[i].transform.position);

            if (_distance < expdetectDistance)
            {
                _nearestExp = _allObjects[i];
                return _nearestExp.transform.position;
            }
            else
            {
                _nearestExp = _allObjects[i];
                return _nearestExp.transform.position;
            }
        }
        return Vector3.zero;
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, expdetectDistance);
    }
}
