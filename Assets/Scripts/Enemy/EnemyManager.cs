using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;


public class EnemyManager : MonoBehaviour
{
    [BoxGroup("Dependent")] [SerializeField] public EnemyPickup pickupScript;
    [BoxGroup("Dependent")] [SerializeField] public NavMeshAgent navMesh;

    [BoxGroup("Properties")] [SerializeField] private float walkSpeed;
    [BoxGroup("Properties")] [SerializeField] private float currentSize;
    
    [BoxGroup("State")] 
    [ShowInInspector]
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
