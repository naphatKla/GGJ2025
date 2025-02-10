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
        [SerializeField] public EnemyType currentType = EnemyType.Normal;
        [SerializeField] private float detectDistance;
        private float _lastDashTime;
        private int _previousLife;
        private int _maxLife;
        
        [BoxGroup("Enemy type")]
        public enum EnemyType
        {
            Normal,
            Tank,
            Piercer,
            Dancer,
            Smoother,
            Pressure
        }
        
        [BoxGroup("State")]
        private enum EnemyState
        {
            Hunting
        }
        #endregion -------------------------------------------------------------------------------------------------------------------

        #region UnityMethods
        protected void Start()
        {
            navMesh = GetComponent<NavMeshAgent>();
            navMesh.updateRotation = false;
            navMesh.updateUpAxis = false;
            navMesh.speed = CurrentSpeed;
            _previousLife = life;
            _maxLife = life;
        }
    
        protected override void Update()
        {
            base.Update();
            if (IsStoppingMovementController) return;
            if (navMesh.enabled) 
                PerformHunting();
        }

        protected override void OnTriggerStay2D(Collider2D other)
        {
            base.OnTriggerStay2D(other);
            if (IsDead) return;
            if (other.CompareTag("Player") && IsDash)
                other.GetComponent<CharacterBase>().TakeDamage(this);
            //Tank Stun
            if (other.CompareTag("Player") && !IsStun && currentType == EnemyType.Tank) StartCoroutine(Stun(0.5f));
        }
        #endregion -------------------------------------------------------------------------------------------------------------------

        #region Methods
        protected override void SkillInputHandler()
        {
            if (currentType == EnemyType.Pressure && navMesh.enabled) { PresureDash(); }
            if (Vector3.Distance(PlayerCharacter.Instance.transform.position, transform.position) > detectDistance) return;
            if (IsStun) return;
            if (currentState != EnemyState.Hunting) return;
            if (currentType == EnemyType.Pressure)
            { navMesh.enabled = false; skillLeft.UseSkill(); return;}
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

        public override void TakeDamage(CharacterBase attacker)
        {
            base.TakeDamage(attacker);
            if (IsDead) return;
            if (_previousLife == life) return;
            int lostLife = _previousLife - life;
            float dropMultiply = (float)lostLife / _maxLife;
            float scoreDrop = score * dropMultiply;
            _previousLife = life;
            Debug.Log($"score = {score}");
            Debug.Log($"score Drop = {scoreDrop}");
            score -= scoreDrop;
            Debug.Log($"score left = {score}");
            DropOxygen(scoreDrop);
        }

        private void PerformHunting()
        {
            if (currentState != EnemyState.Hunting) return;
            if (!PlayerCharacter.Instance) return;
            navMesh.SetDestination(PlayerCharacter.Instance.transform.position);
        }

        private void PresureDash()
        {
            if (Vector3.Distance(PlayerCharacter.Instance.transform.position, transform.position) < detectDistance) return;
            skillRight.UseSkill();
        }
        
        #endregion -------------------------------------------------------------------------------------------------------------------
    }
}
