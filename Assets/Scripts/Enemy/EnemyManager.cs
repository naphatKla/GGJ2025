using System.Collections;
using Characters;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
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
    
    [ShowInInspector]
    private GameObject _target;
    private Coroutine _targetHoldCoroutine;
    private float aiSize;
    private float _targetSize;
    private float lastDashTime = 0f;
    private bool isSkillUsed = false;
    void Start()
    {
        //Get Dependent
        levelScript = GetComponent<EnemyLeveling>();
        huntScript = GetComponent<EnemyHunting>();
        runawayScript = GetComponent<EnemyRunAway>();
        navMesh = GetComponent<NavMeshAgent>();

        //Set AI attribute
        navMesh.updateRotation = false;
        navMesh.updateUpAxis = false;
        navMesh.speed = base.CurrentSpeed;
        
        onSkillPerformed.AddListener(setboolSkill);
        onSkillEnd.AddListener(setboolSkill);
    }

    protected override void SkillInputHandler()
    {
        if (currentState == EnemyState.hunting && Random.value > 0.3f)
        {
            float random = Random.Range(2f, 5f);
            if (Time.time >= lastDashTime + random)
            {
                SkillMouseLeft.UseSkill();
                lastDashTime = Time.time;
            }
        }
        
        if (currentState == EnemyState.runaway && Random.value > 0.3f)
        {
            float random = Random.Range(2f, 5f);
            if (Time.time >= lastDashTime + random)
            {
                SkillMouseLeft.UseSkill();
                lastDashTime = Time.time;
            }
        }
        
        SkillMouseRight.UseSkill();
    }

    protected override void Update()
    {
        base.Update();
        aiSize = BubbleSize;
        huntScript.targetdetectRadius = BubbleSize * 0.1f;
        
        //target lock zone
        SelectTarget();
        
        if( _target != null) { _targetSize = _target.GetComponent<CharacterBase>().BubbleSize; }
        if (navMesh.enabled) PerformLeveling();
        if (_target == null)
        {
            currentState = EnemyState.leveling;
        }
        
        StateDecide();
        if (IsModifyingMovement) return;
        if (navMesh.enabled) PerformHunting();
        if (navMesh.enabled) PerformRunaway();
    }
    
    private void StateDecide()
    {
        //leveling
        //Enemy loose interested after 8s if enemy is chasing at the Outer zone of the screen
        if (!huntScript.EnemyDetectTarget(_target) && currentState == EnemyState.hunting)
        {
            StartCoroutine(LoseInterest());
        }
        //If run away from player scene reset state to leveling
        if (!huntScript.EnemyDetectTarget(_target) && currentState == EnemyState.runaway)
        {
            currentState = EnemyState.leveling;
        }
        
        //hunting
        //If enemy has 10% oxygen more than player current oxygen
        //Enemy would target player if they entered player screen for 0.5s
        if (huntScript.EnemyDetectTarget(_target) && CompareValues(aiSize,_targetSize) > 10 && currentState == EnemyState.leveling)
        {
            StartCoroutine(PreHunting());
        }

        //caution
        //If enemy has oxygen not lower than 7% and not higher than 9% of player current oxygen
        /*if (huntScript.EnemyDetectTarget() 
            && (currentState == EnemyState.hunting || currentState == EnemyState.leveling )
            && (CompareValues(aiSize,_targetSize) > 7 && CompareValues(aiSize,_targetSize) < 9))
        {
            currentState = EnemyState.caution;
        }*/
        //Enemy would stay caution and colleting bubble in a distance
        //If player try to get closer to enemy, it will back off to stay in Outer Zone
        
        //runaway
        //If enemy has 8% oxygen lower than player current oxygen and is entering player screen
        //It waits for 0.5s → 1.5s before trying to run away
        //it will run away until it leaves player border screen
        if (huntScript.EnemyDetectTarget(_target) && aiSize < _targetSize)
        {
            StartCoroutine(PreRunAway());
        }
    }
    private void PerformLeveling()
    {
        if (currentState == EnemyState.leveling && !isSkillUsed)
        {
            navMesh.SetDestination(levelScript.FindNearestExpOrb());
        }
        else if (currentState == EnemyState.leveling && isSkillUsed)
        {
            navMesh.SetDestination(levelScript.FindFarthestExpOrb());
        }
    }
    
    private void PerformHunting()
    {
        if (currentState == EnemyState.hunting && (aiSize >= _targetSize))
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
            if (_target)
            {
                if (NavMesh.SamplePosition(runawayScript.RunFromTarget(_target), out NavMeshHit hit, runawayScript.runDistance, NavMesh.AllAreas))
                {
                    navMesh.SetDestination(hit.position);
                }
            }
            else
            {
                navMesh.SetDestination(levelScript.FindNearestExpOrb());
            }
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
            if (!huntScript.EnemyDetectTarget(_target))
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

        float random = Random.Range(2f, 4f);
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
            if (huntScript.EnemyDetectTarget(_target)) { yield break; }
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        currentState = EnemyState.leveling;
    }

    private void SelectTarget()
    {
        if (_target != null && (_target.activeInHierarchy == false || _target == null))
        {
            _target = null;
            if (_targetHoldCoroutine != null)
            {
                StopCoroutine(_targetHoldCoroutine);
                _targetHoldCoroutine = null;
            }
        }
        
        if (_target == null)
        {
            _target = RadiusDetector();
            if (_target != null)
            {
                if (_targetHoldCoroutine != null)
                {
                    StopCoroutine(_targetHoldCoroutine);
                }
                _targetHoldCoroutine = StartCoroutine(HoldTargetForSeconds(8f));
            }
        }
    }

    private GameObject RadiusDetector()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, huntScript.targetdetectRadius);
        GameObject closestTarget = null;
        float closestSize = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            if ((hit.CompareTag("Player") || hit.CompareTag("Enemy")) && hit.gameObject != gameObject)
            {
                _targetSize = hit.GetComponent<CharacterBase>().BubbleSize;
                if (aiSize > _targetSize)
                {
                    closestTarget = hit.gameObject;
                }
            }
        }

        return closestTarget;
    }
    
    private IEnumerator HoldTargetForSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        _target = null;
        _targetHoldCoroutine = null;
    }

    private void setboolSkill()
    {
        if (!isSkillUsed)
        {
            isSkillUsed = true;
        }
        else
        {
            isSkillUsed = false;
        }
    }
}
