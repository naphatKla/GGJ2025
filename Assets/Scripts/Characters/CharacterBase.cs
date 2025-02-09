using System;
using System.Collections;
using DG.Tweening;
using GlobalSettings;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using Skills;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Characters
{
    public abstract class CharacterBase : MonoBehaviour
    {
        #region Inspectors & Fields
        [SerializeField] protected float score;
        [SerializeField] private float maxSpeed = 6f;
        [SerializeField] protected bool canCollectOxygen;
        [SerializeField] protected bool dropOxygenOnDead = true;
        [SerializeField] protected DeadMode deadMode;
        [SerializeField] [BoxGroup("Life")] protected int life = 1;
        [SerializeField] [BoxGroup("Life")] protected float iframeAfterHitDuration;
        [SerializeField] [BoxGroup("Skills")] protected SkillBase skillLeft;
        [SerializeField] [BoxGroup("Skills")] protected SkillBase skillRight;

        [SerializeField] [ShowIfGroup("PickUpOxygen/" + nameof(canCollectOxygen))] [BoxGroup("PickUpOxygen")]
        private float oxygenDetectionRadius = 5f;

        [SerializeField] [ShowIfGroup("PickUpOxygen/" + nameof(canCollectOxygen))] [BoxGroup("PickUpOxygen")]
        private float oxygenMagneticStartForce = 20f;

        [SerializeField] [ShowIfGroup("PickUpOxygen/" + nameof(canCollectOxygen))] [BoxGroup("PickUpOxygen")]
        private float oxygenMagneticEndForce = 5f;

        [SerializeField] [BoxGroup("Feedbacks")]
        public MMF_Player killFeedback;

        [SerializeField] [BoxGroup("Feedbacks")]
        private MMF_Player takeDamageFeedback;
        
        [SerializeField] [BoxGroup("Feedbacks")]
        private MMF_Player deadFeedback;
        
        [BoxGroup("Events")] [PropertyOrder(100f)]
        public UnityEvent onHit;

        [BoxGroup("Events")] [PropertyOrder(100f)]
        public UnityEvent onHitWithDamage;
        
        [BoxGroup("Events")] [PropertyOrder(100f)]
        public UnityEvent onDead;

        [BoxGroup("Events")] [PropertyOrder(100f)]
        public UnityEvent onPickUpScore;

        private Rigidbody2D _rigidBody2D;
        private Animator _animator;
        private GameObject _cloningParent;
        private float _currentSpeed;
        private float _lastHitTime;
        private float _iframeDuration;
        private float _iframeTimeCounter;
        protected bool IsStun;
        private static readonly int DeadTriggerAnimation = Animator.StringToHash("DeadTrigger");
        private static readonly int DashTriggerAnimation = Animator.StringToHash("DashTrigger");
        private static readonly int BlackHoleTriggerAnimation = Animator.StringToHash("BlackHoleTrigger");

        [Serializable]
        public enum DeadMode
        {
            DestroyObject,
            HideObject
        }
        #endregion -------------------------------------------------------------------------------------------------------------------

        #region Properties
        protected float CurrentSpeed => _currentSpeed;
        public float Score => score;
        public bool IsIframe { get; set; }
        public bool IsStoppingMovementController { get; private set; }
        public bool IsDash { get; set; }
        public bool IsDead { get; protected set; }
        public Animator Animator => _animator;
        protected Rigidbody2D Rigid2D => _rigidBody2D;
        public LayerMask EnemyLayerMask => CharacterGlobalSettings.Instance.EnemyLayerDictionary[tag];
        #endregion -------------------------------------------------------------------------------------------------------------------

        #region UnityMethods
        protected virtual void Awake()
        {
            _currentSpeed = maxSpeed;
            _animator = GetComponent<Animator>();
            _rigidBody2D = GetComponent<Rigidbody2D>();
            skillLeft?.InitializeSkill(this);
            skillRight?.InitializeSkill(this);
            skillLeft?.onSkillStart.AddListener(() => _animator.SetTrigger(DashTriggerAnimation));
            skillRight?.onSkillStart.AddListener(() => _animator.SetTrigger(BlackHoleTriggerAnimation));
        }
        
        protected virtual void Update()
        {
            PullOxygen();
            SkillInputHandler();
            skillLeft?.UpdateCooldown();
            skillRight?.UpdateCooldown();
        }
        
        protected virtual void OnTriggerStay2D(Collider2D other)
        {
            if (!canCollectOxygen) return;
            if (!other.CompareTag("Oxygen")) return;
            Oxygen oxygen = other.GetComponent<Oxygen>();
            if (!oxygen.canPickUp) return;
            AddScore(oxygen.scoreAmount);
            Destroy(other.gameObject);
            onPickUpScore?.Invoke();
        }
        
        private IEnumerator DeadAndDestroy()
        {
            yield return null;
            yield return new WaitUntil(() => _animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1);
            switch (deadMode)
            {
                case DeadMode.DestroyObject:
                    Destroy(gameObject);
                    break;
                case DeadMode.HideObject:
                    gameObject.SetActive(false);
                    break;
            }
        }
        #endregion -------------------------------------------------------------------------------------------------------------------

        #region Methods
        protected abstract void SkillInputHandler();

        protected virtual void SetScore(float scoreToSet)
        {
            score = scoreToSet;
        }

        public virtual void AddScore(float scoreToAdd)
        {
            score += scoreToAdd;
            if (scoreToAdd >= 0) return;
            DropOxygen(Mathf.Abs(scoreToAdd));
        }

        public virtual void TakeDamage(CharacterBase attacker)
        {
            onHit?.Invoke();
            if (IsIframe) return;
            if (IsDead) return;
            if (Time.time - _lastHitTime < iframeAfterHitDuration) return;

            life--;
            _lastHitTime = Time.time;
            takeDamageFeedback?.PlayFeedbacks();
            onHitWithDamage?.Invoke();
            if (life > 0) return;
            Dead(attacker);
        }
        
        public virtual void ForceDead(CharacterBase attacker)
        {
            dropOxygenOnDead = false;
            Dead(attacker);
        }

        public void StartMovementController()
        {
            IsStoppingMovementController = false;
            if (TryGetComponent(out NavMeshAgent navmesh))
            {
                navmesh.enabled = true;
            }
        }

        public void StopMovementController()
        {
            IsStoppingMovementController = true;
            if (CompareTag("Player"))
            {
                _rigidBody2D.velocity = Vector2.zero;
                return;
            }

            if (TryGetComponent(out NavMeshAgent navmesh))
            {
                navmesh.enabled = false;
            }
        }

        public Vector2 ClampMovePositionToBound(Vector2 destination)
        {
            Vector2 direction = (destination - (Vector2)transform.position).normalized;
            float distance = Vector2.Distance(transform.position, destination);
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, CharacterGlobalSettings.Instance.LevelBoundLayerMask);
            if (hit.collider) 
                destination = (Vector2)transform.position + (direction * hit.distance);
            return destination;
        }
        
        protected virtual IEnumerator Stun(float duration)
        {
            if (IsIframe) yield break;
            if (IsDead) yield break;
            if (IsStun) yield break;

            IsStun = true;
            StopMovementController();
            yield return new WaitForSeconds(duration);
            StartMovementController();
            IsStun = false;
        }

        protected virtual void Dead(CharacterBase attacker)
        {
            if (IsDead) return;
            IsDead = true;
            if (dropOxygenOnDead) DropOxygen(score);
            if (CompareTag("Player")) _rigidBody2D.velocity = Vector2.zero;
            else if (CompareTag("Enemy"))
            {
                if (attacker && attacker.CompareTag("Player")) PlayerCharacter.HitCombo++;
                TryGetComponent(out NavMeshAgent navmesh);
                navmesh.velocity = Vector3.zero;
                navmesh.enabled = false;
            }

            if (attacker is CloningCharacter)
                attacker.GetComponent<CloningCharacter>().OwnerCharacter.killFeedback?.PlayFeedbacks();
            else attacker?.killFeedback?.PlayFeedbacks();
            _animator.SetTrigger(DeadTriggerAnimation);
            _animator.Play("Dead");
            deadFeedback?.PlayFeedbacks();
            onDead?.Invoke();
            StartCoroutine(DeadAndDestroy());
        }

        protected virtual void PullOxygen()
        {
            if (!canCollectOxygen) return;
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position,
                (transform.localScale.x / 2) + oxygenDetectionRadius, LayerMask.GetMask("Oxygen"));
            foreach (Collider2D col in colliders)
            {
                Vector2 direction = (transform.position - col.transform.position).normalized;
                Vector2 perpendicularRight = new Vector2(direction.y, -direction.x).normalized;
                Vector2 combinedVector = (direction + perpendicularRight).normalized;
                float force = oxygenMagneticStartForce - (Time.deltaTime * 3);
                force = Mathf.Clamp(force, oxygenMagneticEndForce, oxygenMagneticStartForce);
                col.transform.position += (Vector3)(combinedVector * (force * Time.deltaTime));
            }
        }

        protected virtual void DropOxygen(float amount)
        {
            float sumDrop = 0;
            foreach (Oxygen drop in OxygenSpawnManager.Instance.AllOfOxygenType)
            {
                while (sumDrop + drop.scoreAmount <= amount)
                {
                    sumDrop += drop.scoreAmount;
                    Transform characterTransform = transform;
                    float radius = characterTransform.localScale.x * 2f;
                    Vector2 randomPosition = characterTransform.position +
                                             new Vector3(Random.Range(-radius, radius), Random.Range(-radius, radius),
                                                 0);
                    Instantiate(drop.gameObject, transform.position, Quaternion.identity)
                        .TryGetComponent(out Oxygen oxygenDrop);
                    oxygenDrop.transform.DOMove(randomPosition, 0.4f).SetEase(Ease.InOutSine).onComplete +=
                        () => oxygenDrop.canPickUp = true;
                    oxygenDrop.canPickUp = false;
                }
            }
        }

        public virtual CloningCharacter CreateCloning(float lifeTime, CloningCharacter.LifeTimeType endType,
            int cloneLife, bool dealDamageOnTouch, bool destroyOnTouch)
        {
            if (!_cloningParent) _cloningParent = new GameObject("CloningParent");
            _cloningParent.transform.position = transform.position;

            GameObject newCloning = Instantiate(gameObject, _cloningParent.transform.position, Quaternion.identity,
                _cloningParent.transform);
            MonoBehaviour[] scripts = newCloning.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
                Destroy(script);

            CloningCharacter cloneChar = newCloning.AddComponent<CloningCharacter>();
            if (cloneChar.TryGetComponent(out NavMeshAgent agent))
                agent.enabled = false;
            cloneChar.Initialize(this, cloneLife, lifeTime, endType, dealDamageOnTouch, destroyOnTouch,
                canCollectOxygen);
            cloneChar.SetScore(0);
            cloneChar.tag = tag;
            return cloneChar;
        }
        
#if UNITY_EDITOR // Editor specific code
        [PropertyOrder(1000), Title(""), Button(ButtonSizes.Large), GUIColor(0, 1, 0)] 
        private void OpenGlobalSettings()
        {
            Sirenix.OdinInspector.Editor.OdinEditorWindow.InspectObject(CharacterGlobalSettings.Instance);
        }
#endif
        #endregion -------------------------------------------------------------------------------------------------------------------
    }
}