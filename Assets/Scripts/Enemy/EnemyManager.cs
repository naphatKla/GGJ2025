using System;
using System.Collections;
using System.Collections.Generic;
using Characters;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;


public class EnemyManager : CharacterBase
{
    [BoxGroup("Dependent")] [SerializeField] public EnemyLeveling levelScript;
    [BoxGroup("Dependent")] [SerializeField] public EnemyHunting huntScript;
    [BoxGroup("Dependent")] [SerializeField] public EnemyRunAway runawayScript;
    [BoxGroup("Dependent")] [SerializeField] public NavMeshAgent navMesh;
    
    [BoxGroup("State")] 
    private enum EnemyState { leveling, hunting, caution, runaway}
    [SerializeField] private EnemyState currentState = EnemyState.leveling;
    
    private GameObject _target;
    private float aiSize;
    private float playerSize;
    void Start()
    {
        //Get Dependent
        levelScript = GetComponent<EnemyLeveling>();
        huntScript = GetComponent<EnemyHunting>();
        runawayScript = GetComponent<EnemyRunAway>();
        navMesh = GetComponent<NavMeshAgent>();
        _target = GameObject.FindGameObjectWithTag("Player");
        
        //Set AI attribute
        navMesh.updateRotation = false;
        navMesh.updateUpAxis = false;
    }

    protected override void SkillInputHandler()
    {
        /*SkillMouseLeft?.UseSkill();
        SkillMouseRight?.UseSkill();*/
    }

    protected override void Update()
    {
        base.Update();
        aiSize = BubbleSize;
        playerSize = _target.GetComponent<CharacterBase>().BubbleSize;
        navMesh.velocity = Vector2.ClampMagnitude(navMesh.velocity, Speed);
        
        StateDecide();
        if (IsModifyingMovement) return;
        PerformLeveling();
        PerformHunting();
        //PerformCaution();
        PerformRunaway();
    }
    
    private void StateDecide()
    {
        //leveling
        //Enemy loose interested after 8s if enemy is chasing at the Outer zone of the screen
        if (!huntScript.EnemyDetectPlayer() && currentState == EnemyState.hunting)
        {
            StartCoroutine(LoseInterest());
        }
        //If run away from player scene reset state to leveling
        if (!huntScript.EnemyDetectPlayer() && currentState == EnemyState.runaway)
        {
            currentState = EnemyState.leveling;
        }
        
        //hunting
        //If enemy has 10% oxygen more than player current oxygen
        //Enemy would target player if they entered player screen for 0.5s
        if (huntScript.EnemyDetectPlayer() && CompareValues(aiSize,playerSize) > 10 && currentState == EnemyState.leveling)
        {
            navMesh.ResetPath();
            StartCoroutine(PreHunting());
        }

        //caution
        //If enemy has oxygen not lower than 7% and not higher than 9% of player current oxygen
        /*if (huntScript.EnemyDetectPlayer() 
            && (currentState == EnemyState.hunting || currentState == EnemyState.leveling )
            && (CompareValues(aiSize,playerSize) > 7 && CompareValues(aiSize,playerSize) < 9))
        {
            currentState = EnemyState.caution;
        }*/
        //Enemy would stay caution and colleting bubble in a distance
        //If player try to get closer to enemy, it will back off to stay in Outer Zone
        
        //runaway
        //If enemy has 8% oxygen lower than player current oxygen and is entering player screen
        //It waits for 0.5s → 1.5s before trying to run away
        //it will run away until it leaves player border screen
        if (huntScript.EnemyDetectPlayer() && aiSize < playerSize)
        {
            StartCoroutine(PreRunAway());
        }
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
        if (currentState == EnemyState.hunting && (aiSize >= playerSize))
        {
            navMesh.SetDestination(_target.transform.position);
        }
    }
    
    /*private void PerformCaution()
    {
        if (currentState == EnemyState.caution)
        {
            if (NavMesh.SamplePosition(cautionScript.CautionFromTarget(), out NavMeshHit hit, cautionScript.innerRadius, NavMesh.AllAreas))
            {
                navMesh.SetDestination(hit.position);
            }
            else
            {
                navMesh.SetDestination(levelScript.FindNearestExpOrb());
            }
        }
    }*/
    
    private void PerformRunaway()
    {
        if (currentState == EnemyState.runaway)
        {
            if (NavMesh.SamplePosition(runawayScript.RunFromTarget(), out NavMeshHit hit, runawayScript.runDistance, NavMesh.AllAreas))
            {
                navMesh.SetDestination(hit.position);
            }
        }
    }
    
    private void PerformSkill()
    {
        if (currentState == EnemyState.hunting)
        {
            
        }
    }
    
    private float CompareValues(float aiValue, float playerValue)
    {
        if (aiValue == playerValue)
        { return 0; }

        float difference;

        if (aiValue > playerValue)
        {
            difference = ((aiValue - playerValue) / playerValue) * 100;
            return difference;
        }
        else
        {
            difference = ((playerValue - aiValue) / aiValue) * 100;
            return -difference;
        }

        return 0;
    }
    
    private IEnumerator PreHunting()
    {
        float elapsedTime = 0f;

        while (elapsedTime < huntScript.timebeforeHunting)
        {
            //if player out of range
            if (!huntScript.EnemyDetectPlayer())
            {
                yield break;
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        currentState = EnemyState.hunting;
    }
    
    private IEnumerator PreRunAway()
    {
        float elapsedTime = 0f;

        float random = Random.Range(0.5f, 1.5f);
        while (elapsedTime < random)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        currentState = EnemyState.runaway;
    }

    
    private IEnumerator LoseInterest()
    {
        float elapsedTime = 0f;

        while (elapsedTime < huntScript.timeloseInterest)
        {
            //if player out of range
            if (huntScript.EnemyDetectPlayer()) { yield break; }
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        currentState = EnemyState.leveling;
    }

}
