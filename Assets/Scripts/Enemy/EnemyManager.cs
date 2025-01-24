using System;
using System.Collections;
using System.Collections.Generic;
using Characters;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;


public class EnemyManager : CharacterBase
{
    [BoxGroup("Dependent")] [SerializeField] public EnemyLeveling levelScript;
    [BoxGroup("Dependent")] [SerializeField] public EnemyHunting huntScript;
    [BoxGroup("Dependent")] [SerializeField] public NavMeshAgent navMesh;

    [BoxGroup("Properties")] [SerializeField] private float walkSpeed;
    [BoxGroup("Properties")] [SerializeField] private float loseInterestTimer;

    [BoxGroup("State")] 
    private enum EnemyState { leveling, hunting, caution, runaway}
    [SerializeField] private EnemyState currentState = EnemyState.leveling;
    
    private GameObject _target;
    void Start()
    {
        //Get Dependent
        levelScript = GetComponent<EnemyLeveling>();
        huntScript = GetComponent<EnemyHunting>();
        navMesh = GetComponent<NavMeshAgent>();
        _target = GameObject.FindGameObjectWithTag("Player");
        
        //Set AI attribute
        navMesh.speed = walkSpeed;
        navMesh.updateRotation = false;
        navMesh.updateUpAxis = false;
    }

    protected override void SkillInputHandler()
    {
        throw new NotImplementedException();
    }

    void Update()
    {
        
    }

    private void StateDecide()
    {
        //leveling
        //Enemy loose interested after 8s if enemy is chasing at the Outer zone of the screen
        
        //hunting
        if (currentState != EnemyState.leveling) { return; }
        //If enemy has 10% oxygen more than player current oxygen
        
        //Enemy would target player if they entered player screen for 0.5s
        if (huntScript.EnemyDetectPlayer() )
        {
            StartCoroutine(PreHunting());
        }
        //If enemy stay in the Inner zone it will chase player forever until it leaves inner zone
        
        //caution
        //If enemy has oxygen not lower than 7% and not higher than 9% of player current oxygen
        //Enemy would stay caution and colleting bubble in a distance
        //If player try to get closer to enemy, it will back off to stay in Outer Zone
        
        //runaway
        //If enemy has 8% oxygen lower than player current oxygen and is entering player screen
        //It waits for 0.5s → 1.5s before trying to run away
        //it will run away until it leaves player border screen
    }
    private void PerformLeveling()
    {
        if (currentState == EnemyState.leveling)
        {
            navMesh.SetDestination(levelScript.FindNearestExpOrb());
        }
    }
    
    private void PerformHunting()
    {
        if (currentState == EnemyState.hunting)
        {
            navMesh.SetDestination(_target.transform.position);
        }
    }
    
    private void PerformCaution()
    {
        if (currentState == EnemyState.caution)
        {
            
        }
    }
    
    private void PerformRunaway()
    {
        if (currentState == EnemyState.runaway)
        {
            
        }
    }
    
    public IEnumerator PreHunting()
    {
        float elapsedTime = 0f;

        while (elapsedTime < huntScript.timebeforeHunting)
        {
            //if player out of range
            if (!huntScript.EnemyDetectPlayer()) { yield break; }
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        currentState = EnemyState.hunting;
    }
    
    public IEnumerator LoseInterest()
    {
        float elapsedTime = 0f;

        while (elapsedTime < huntScript.timeloseInterest)
        {
            //if player out of range
            if (!huntScript.EnemyDetectPlayer()) { yield break; }
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        currentState = EnemyState.hunting;
    }

}
