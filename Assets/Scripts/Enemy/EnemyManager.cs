using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class EnemyManager : MonoBehaviour
{
    [SerializeField] public EnemyPickup pickupScript;

    [SerializeField] private float walkSpeed;
    [SerializeField] private float currentSize;
    [SerializeField] private enum EnemyState { leveling, hunting, escape, useSkill}
    
    void Start()
    {
        //Get Dependent
        pickupScript = GetComponent<EnemyPickup>();
    }
    
    void Update()
    {
        
    }
}
