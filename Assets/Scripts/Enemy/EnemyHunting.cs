using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHunting : MonoBehaviour
{
    [SerializeField] public float targetdetectRadius;
    [SerializeField] public float timebeforeHunting = 2f;
    [SerializeField] public float timeloseInterest = 8f;
    private GameObject _target;

    private void Start()
    {
        _target = GameObject.FindGameObjectWithTag("Player");
    }

    public bool EnemyDetectPlayer()
    {
        if (_target == null) { return false; }
        
        float _distance = Vector2.Distance(this.transform.position ,_target.transform.position);
        
        if (_distance <= targetdetectRadius)
        {
            return true;
        }

        return false;
    }
    
    
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, targetdetectRadius);
    }
}
