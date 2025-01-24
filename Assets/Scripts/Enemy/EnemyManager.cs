using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class EnemyManager : MonoBehaviour
{
    [SerializeField] public EnemyPickup pickupScript;
    [SerializeField] public NavMeshAgent navMesh;

    [SerializeField] private float walkSpeed;
    [SerializeField] private float currentSize;
    [SerializeField] private enum EnemyState { leveling, hunting, escape, useSkill}
    
    void Start()
    {
        //Get Dependent
        pickupScript = GetComponent<EnemyPickup>();
        navMesh = GetComponent<NavMeshAgent>();
        
        //Set AI attribute
        navMesh.speed = walkSpeed;
        navMesh.updateRotation = false;
        navMesh.updateUpAxis = false;
    }
    
    void Update()
    {
        navMesh.SetDestination(pickupScript.FindNearestExpOrb());
    }
}
