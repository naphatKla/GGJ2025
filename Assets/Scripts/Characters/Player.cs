using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Characters
{
    public class Player : CharacterBase
    {
        #region Inspectors & Fields 
        [SerializeField] [BoxGroup("PickUpOxygen")] private float oxygenDetectionRadius = 2f;
        [SerializeField] [BoxGroup("PickUpOxygen")] private float oxygenMagneticStartForce = 9f;
        [SerializeField] [BoxGroup("PickUpOxygen")] private float oxygenMagneticEndForce = 2f;
        [BoxGroup("Events")] [PropertyOrder(100f)] public UnityEvent onPickUpScore;
        #endregion ----------------------------------------------------------------------------------------------------------------------------------------
        
        #region Properties 
        public static float HitCombo;
        public static Player Instance { get; private set; }
        #endregion ----------------------------------------------------------------------------------------------------------------------------------------
        
        #region UnityMethods

        private void OnEnable()
        {
            onDead?.AddListener(ResetHitCombo);
        }

        private void OnDisable()
        {
            onDead?.RemoveListener(ResetHitCombo);
        }

        protected override void Awake()
        {
            if (!Instance)
                Instance = this;
            base.Awake();
        }
        
        protected override void Update()
        {
            base.Update();
            MovementController();
            PullOxygen();
        }
        
        protected virtual void OnTriggerStay2D(Collider2D other)
        {
            if (IsDead) return;
            if (other.CompareTag("Enemy") && IsDash) 
                other.GetComponent<EnemyManager>().Dead(this);
            
            if (!other.CompareTag("Oxygen")) return;
            Oxygen oxygen = other.GetComponent<Oxygen>();
            if (!oxygen.canPickUp) return;
            AddScore(oxygen.scoreAmount);
            Destroy(other.gameObject);
            onPickUpScore?.Invoke();
        }
        #endregion ----------------------------------------------------------------------------------------------------------------------------------------

        #region Methods
        protected override void SkillInputHandler()
        {
            if (Time.timeScale == 0) return;
            if (Input.GetMouseButtonDown(0))
                skillLeft.UseSkill();
            if (Input.GetMouseButtonDown(1))
                skillRight.UseSkill();
        }
        
        public override void Dead(CharacterBase killer, bool dropOxygen = true)
        {
            if (IsDash) return;
            base.Dead(killer, false);
        }
        
        private static void ResetHitCombo()
        {
            HitCombo = 0f;
        }
        
        private void MovementController()
        {
            if (IsModifyingMovement) return;
            Vector2 mouseDirection = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
            Rigid2D.AddForce(mouseDirection.normalized * CurrentSpeed);
            Rigid2D.velocity = Vector2.ClampMagnitude(Rigid2D.velocity, CurrentSpeed);
        }
        
        private void PullOxygen()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, (transform.localScale.x / 2) + oxygenDetectionRadius, LayerMask.GetMask("Oxygen"));
            foreach (Collider2D col in colliders)
            {
                Vector2 direction = (transform.position - col.transform.position).normalized;
                Vector2 perpendicularRight = new Vector2(direction.y, -direction.x).normalized;
                Vector2 combinedVector = (direction + perpendicularRight).normalized;
                float force = oxygenMagneticStartForce - (Time.deltaTime*3);
                force = Mathf.Clamp(force, oxygenMagneticEndForce, oxygenMagneticStartForce);
                col.transform.position += (Vector3)(combinedVector * (force * Time.deltaTime));
            }
        }
        #endregion ----------------------------------------------------------------------------------------------------------------------------------------
    }
}
