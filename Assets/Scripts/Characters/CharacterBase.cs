using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using Skills;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Characters
{
    public abstract class CharacterBase : MonoBehaviour
    {
        [SerializeField] private float bubbleSize = 1f;
        [SerializeField] private float maxSize = 10000f;
        [SerializeField] private float maxSpeed = 6f;
        [SerializeField] private float minSpeed = 2f;
        [SerializeField] [PropertyTooltip("1 bubble size will affect to the object scale += 0.01")] [BoxGroup("Upgrade")] private float increaseScalePerSize = 0.01f; 
        [SerializeField] [BoxGroup("PickUpOxygen")] private float oxygenDetectionRadius = 2f;
        [SerializeField] [BoxGroup("PickUpOxygen")] private float oxygenMagneticStartForce = 9f;
        [SerializeField] [BoxGroup("PickUpOxygen")] private float oxygenMagneticEndForce = 2f;
        [SerializeField] [BoxGroup("PickUpOxygen")] private float oxygenCurveForce = 5f;
        [SerializeField] [BoxGroup("Skills")] protected SkillBase SkillMouseLeft;
        [SerializeField] [BoxGroup("Skills")] protected  SkillBase SkillMouseRight;
        [SerializeField] [BoxGroup("Feedbacks")] private MMF_Player sizeUpFeedback;
        [SerializeField] [BoxGroup("Feedbacks")] private MMF_Player sizeDownFeedback;
        [SerializeField] [BoxGroup("Feedbacks")] private MMF_Player explodeFeedback;
        [SerializeField] [BoxGroup("Feedbacks")] private MMF_Player mergeFeedback;
        [SerializeField] [BoxGroup("Feedbacks")] private MMF_Player deadFeedback;
        private SpriteRenderer _spriteRenderer;
        private Collider2D _collider2D;
        private TrailRenderer _trailRenderer;
        [HideInInspector] public Rigidbody2D rigidbody2D;
        protected float lastStateSize = 100f;
        [ShowInInspector] protected float currentSpeed;
        private List<CloningCharacter> clones = new List<CloningCharacter>();
        private GameObject _cloningParent;
        protected Animator Animator;
        
        public float BubbleSize => bubbleSize;
        protected float CurrentSpeed => currentSpeed;
        public bool IsModifyingMovement { get; set; }
        protected abstract void SkillInputHandler();
        [Title("Events")] public UnityEvent onSizeUpState;
     
        
        protected virtual void Awake()
        {
            SkillMouseLeft?.InitializeSkill(this);
            SkillMouseRight?.InitializeSkill(this);
            rigidbody2D = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _collider2D = GetComponent<Collider2D>();
            _trailRenderer = GetComponent<TrailRenderer>();
            currentSpeed = maxSpeed;
        }
        
        protected virtual void Update()
        {
            SkillInputHandler();
            SkillMouseLeft?.UpdateCooldown();
            SkillMouseRight?.UpdateCooldown();
            
            // เก็บ oxygen รอบๆรัศมี
            if (!_spriteRenderer.enabled) return;
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, (transform.localScale.x / 2) + oxygenDetectionRadius, LayerMask.GetMask("EXP"));
            
            foreach (var collider in colliders)
            {
                Vector2 direction = (transform.position - collider.transform.position).normalized;
                Vector2 perpendicularRight = new Vector2(direction.y, -direction.x).normalized;
                Vector2 combinedVector = (direction + perpendicularRight).normalized;
                float force = oxygenMagneticStartForce - (Time.deltaTime*3);
                force = Mathf.Clamp(force, oxygenMagneticEndForce, oxygenMagneticStartForce);
                collider.transform.position += (Vector3)(combinedVector * force * Time.deltaTime);
            }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Exp")) return;
            ExpScript exp = other.GetComponent<ExpScript>();
            if (!exp.canPickUp) return;
            
            AddSize(exp.expAmount);
            Destroy(other.gameObject);
            return;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            CloningCharacter cloningCharacter = other.GetComponent<CloningCharacter>();
            CloningCharacter thisCharacter = GetComponent<CloningCharacter>();
            
            if (cloningCharacter && cloningCharacter.OwnerCharacter == this) return;
            if (thisCharacter && thisCharacter.OwnerCharacter == other.GetComponent<CharacterBase>()) return;
            if (thisCharacter && cloningCharacter && (thisCharacter.OwnerCharacter == cloningCharacter.OwnerCharacter)) return;
                
            float distance = Vector2.Distance(transform.position, other.transform.position);
            float thisRadius = (transform.localScale.x / 2);
            CharacterBase otherCharacter = other.GetComponent<CharacterBase>();
            if (!otherCharacter) return;
            bool canEat = (distance <= thisRadius) && (bubbleSize > otherCharacter.bubbleSize);
            if (!canEat) return;
            AddSize(otherCharacter.bubbleSize);
            otherCharacter.Dead();
        }
        
        protected virtual void Dead()
        {
            DropOxygen(bubbleSize);
            deadFeedback?.PlayFeedbacks();
            Destroy(gameObject);
        }

        protected virtual void DropOxygen(float amount)
        {
            float sumDrop = 0;
            foreach (ExpScript drop in RandomSpawnExp.Instance.OxygenAvailable)
            {
                while (sumDrop + drop.expAmount <= amount)
                {
                    sumDrop += drop.expAmount;
                    float radius = transform.localScale.x;
                    Vector2 randomPosition = transform.position + new Vector3(Random.Range(-radius, radius), Random.Range(-radius, radius), 0);
                    ExpScript dropInstant = Instantiate(drop.gameObject, transform.position, Quaternion.identity).GetComponent<ExpScript>();
                    dropInstant.canPickUp = false;
                    dropInstant.transform.DOMove(randomPosition, 0.4f).SetEase(Ease.InOutSine).onComplete += () => dropInstant.canPickUp = true;
                }
            }
        }
        
        public virtual void SetSize(float size)
        {
            bubbleSize = size;
            UpdateScaleAndSpeed();
        }
        
        public virtual void AddSize(float size)
        {
            bubbleSize += size;
            if (Mathf.Abs(bubbleSize - lastStateSize) >= 100) 
            {
                onSizeUpState?.Invoke();
                lastStateSize = bubbleSize;
            }
            
            switch (size)
            {
                case > 0:
                    sizeUpFeedback?.PlayFeedbacks();
                    break;
                case < 0:
                    sizeDownFeedback?.PlayFeedbacks();
                    DropOxygen(Mathf.Abs(size));
                    break;
            }
            UpdateScaleAndSpeed();
            if (bubbleSize > 0) return;
            Dead();
        }

        public void UpdateScaleAndSpeed()
        {
            float size = Mathf.Clamp((bubbleSize * increaseScalePerSize), 0, 100f);
            Vector2 newScale = Vector2.one * size;
            currentSpeed = Mathf.Lerp(minSpeed, maxSpeed, 1 - (bubbleSize / maxSize));
            currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);
            
            transform.DOScale(newScale, 0.05f).SetEase(Ease.OutBounce).OnComplete(() =>
            {
                _trailRenderer.startWidth = transform.localScale.x;
            });
        }

        [Button]
        public void ExplodeOut8Direction(float force, float mergeTime)
        {
            Vector2[] directions = new Vector2[8];
            _cloningParent = new GameObject("CloningParent");
            _cloningParent.transform.position = transform.position;
            explodeFeedback?.PlayFeedbacks();
            
            for (int i = 0; i < 8; i++)
            {
                directions[i] = new Vector2(Mathf.Cos(i * Mathf.PI / 4), Mathf.Sin(i * Mathf.PI / 4));
                Vector2 position = _cloningParent.transform.position + ((Vector3)directions[i] * (transform.localScale.x + force));
                GameObject obj = Instantiate(gameObject, _cloningParent.transform.position, Quaternion.identity, _cloningParent.transform);
                
                MonoBehaviour[] scripts = obj.GetComponents<MonoBehaviour>();
                foreach (MonoBehaviour script in scripts) 
                    script.enabled = false;
                
                obj.AddComponent(typeof(CloningCharacter));
                CloningCharacter clone = obj.GetComponent<CloningCharacter>();
                clones.Add(clone);
                clone.OwnerCharacter = this;
                clone.SizeGained = 0f;

                clone.transform.DOMove(position, 0.25f).SetEase(Ease.InOutSine);
                clone.SetSize(bubbleSize / 3f);
            }
            
            StartCoroutine(CloningFollowAndMergedBack(mergeTime));
        }

        IEnumerator CloningFollowAndMergedBack(float time)
        {
            float timeCounter = 0;
            _spriteRenderer.enabled = false;
            _collider2D.enabled = false;
            _trailRenderer.enabled = false;
            
            while (timeCounter < time)
            {
                _cloningParent.transform.position = Vector2.Lerp(_cloningParent.transform.position, transform.position, timeCounter / time);
                timeCounter += Time.deltaTime;
                yield return null;
            }

            mergeFeedback?.PlayFeedbacks();
            foreach (CloningCharacter clone in clones)
            {
                if (!clone) continue;
                yield return new WaitForSeconds(0.02f);
                clone.transform.DOMove(transform.position, 0.25f).SetEase(Ease.InOutSine).OnComplete(() =>
                {
                    AddSize(clone.SizeGained);
                    _spriteRenderer.enabled = true;
                    _collider2D.enabled = true;
                    _trailRenderer.enabled = true;
                    Destroy(clone.gameObject,0.02f);
                });
            }
            yield return new WaitForSeconds(0.3f);
            Destroy(_cloningParent);
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, (transform.localScale.x/2) + oxygenDetectionRadius);
        }
    }
}
