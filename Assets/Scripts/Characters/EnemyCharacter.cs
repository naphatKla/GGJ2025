using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Characters
{
    public class EnemyCharacter : CharacterBase
    {
        #region Inspectors & Fields
        [SerializeField] private NavMeshAgent navMesh;
        [SerializeField] private EnemyState currentState = EnemyState.Hunting;
        private float _lastDashTime;
        
        [BoxGroup("State")]
        private enum EnemyState
        {
            Hunting
        }
        #endregion -------------------------------------------------------------------------------------------------------------------

        #region UnityMethods
        protected override void Start()
        {
            base.Start();
            navMesh = GetComponent<NavMeshAgent>();
            navMesh.updateRotation = false;
            navMesh.updateUpAxis = false;
            navMesh.speed = CurrentSpeed;
        }
    
        protected override void Update()
        {
            base.Update();
            if (IsModifyingMovement) return;
            if (navMesh.enabled) 
                PerformHunting();
        }

        protected override void OnTriggerStay2D(Collider2D other)
        {
            base.OnTriggerStay2D(other);
            if (IsDead) return;
            if (other.CompareTag("Player") && IsDash)
                other.GetComponent<CharacterBase>().TakeDamage(this);
        }
        #endregion -------------------------------------------------------------------------------------------------------------------

        #region Methods
        protected override void SkillInputHandler()
        {
            if (currentState != EnemyState.Hunting) return;
            if (Random.value >= 0f) // change this value > 0 to test another skill
            {
                float random = Random.Range(2f, 5f);
                if (Time.time >= _lastDashTime + random)
                {
                    skillLeft.UseSkill();
                    _lastDashTime = Time.time;
                }
            }
            else
            {
                float random = Random.Range(2f, 5f);
                if (Time.time >= _lastDashTime + random)
                {
                    skillRight.UseSkill();
                    _lastDashTime = Time.time;
                }
            }
        }

        private void PerformHunting()
        {
            if (currentState != EnemyState.Hunting) return;
            if (!PlayerCharacter.Instance) return;
            navMesh.SetDestination(PlayerCharacter.Instance.transform.position);
        }
        #endregion -------------------------------------------------------------------------------------------------------------------
    }
}
