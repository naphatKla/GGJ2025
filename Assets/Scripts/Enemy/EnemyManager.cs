using Characters;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class EnemyManager : CharacterBase
{
    [BoxGroup("Dependent")] [SerializeField] public NavMeshAgent navMesh;

    [BoxGroup("State")] 
    private enum EnemyState {Hunting}
    [SerializeField] private EnemyState currentState = EnemyState.Hunting;
    private float _lastDashTime = 0f;

    protected override void Start()
    {
        base.Start();
        navMesh = GetComponent<NavMeshAgent>();

        //Set AI attribute
        navMesh.updateRotation = false;
        navMesh.updateUpAxis = false;
        navMesh.speed = base.CurrentSpeed;
    }
    
    protected override void Update()
    {
        base.Update();
        if (IsModifyingMovement) return;
        if (navMesh.enabled) PerformHunting();
    }
    
    private void OnTriggerStay2D(Collider2D other)
    {
        if (IsDead) return;
        if (other.CompareTag("Player") && IsDash)
            Player.Instance.Dead(this);
    }

    protected override void SkillInputHandler()
    {
        if (currentState != EnemyState.Hunting) return;
        if (Random.value >= 0f)
        {
            float random = Random.Range(2f, 5f);
            if (Time.time >= _lastDashTime + random)
            {
                skillLeft.UseSkill();
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
    
    private void PerformHunting()
    {
        if (currentState != EnemyState.Hunting) return;
        if (Player.Instance == null) return;
        navMesh.SetDestination(Player.Instance.transform.position);
    }
}
