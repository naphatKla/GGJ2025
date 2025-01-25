using System.Collections;
using Characters;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;



public class EnemyManager : CharacterBase
{
    [BoxGroup("Dependent")] [SerializeField] public EnemyHunting huntScript;
    [BoxGroup("Dependent")] [SerializeField] public NavMeshAgent navMesh;

    [BoxGroup("State")] 
    private enum EnemyState {Hunting}
    [SerializeField] private EnemyState currentState = EnemyState.Hunting;
    private float _lastDashTime = 0f;
  
    void Start()
    {
        //Get Dependent
        huntScript = GetComponent<EnemyHunting>();
        navMesh = GetComponent<NavMeshAgent>();

        //Set AI attribute
        navMesh.updateRotation = false;
        navMesh.updateUpAxis = false;
        navMesh.speed = base.CurrentSpeed;
    }

    protected override void SkillInputHandler()
    {
        if (currentState != EnemyState.Hunting) return;
        if (Random.value >= 0f)
        {
            float random = Random.Range(2f, 5f);
            if (Time.time >= _lastDashTime + random)
            {
                SkillMouseLeft.UseSkill();
                _lastDashTime = Time.time;
            }
        }
        /*else
        {
            float random = Random.Range(2f, 5f);
            if (Time.time >= _lastDashTime + random)
            {
                SkillMouseRight.UseSkill();
                _lastDashTime = Time.time;
            }
        }*/
    }

    protected override void Update()
    {
        base.Update();
        if (IsModifyingMovement) return;
        if (navMesh.enabled) PerformHunting();
    }
    
    private void PerformHunting()
    {
        if (currentState != EnemyState.Hunting) return;
        navMesh.SetDestination(Player.Instance.transform.position);
    }
    
    private IEnumerator PreHunting()
    {
        float elapsedTime = 0f;

        while (elapsedTime < huntScript.timebeforeHunting)
        {
            //if player out of range
            if (!huntScript.EnemyDetectTarget(_player))
            {
                yield break;
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        currentState = EnemyState.Hunting;
        if (navMesh.hasPath) navMesh.ResetPath();
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
            if (huntScript.EnemyDetectTarget(_player)) { yield break; }
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        currentState = EnemyState.leveling;
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
    
    private IEnumerator HoldTargetForSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        _player = null;
        _targetHoldCoroutine = null;
    }
    
}
