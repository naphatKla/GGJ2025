using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class EnemyPickup : MonoBehaviour
{
    [SerializeField] private float detectDistance;
    [SerializeField] public Transform expParent;

    private GameObject[] AllObjects;
    private float _distance;
    [ShowInInspector]
    private GameObject _nearestExp;

    public Vector3 FindNearestExpOrb()
    {
        AllObjects = GameObject.FindGameObjectsWithTag("Exp");
        for (int i = 0; i < expParent.childCount; i++)
        {
            _distance = Vector2.Distance(this.transform.position ,AllObjects[i].transform.position);

            if (_distance < detectDistance)
            {
                _nearestExp = AllObjects[i];
                return _nearestExp.transform.position;
            }
        }
        return Vector3.zero;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Exp"))
        {
            Destroy(other.gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectDistance);
    }
}
