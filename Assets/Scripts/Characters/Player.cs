using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Characters
{
    public class Player : CharacterBase
    {
        #region Inspectors & Fields 
        [SerializeField] [BoxGroup("PickUpOxygen")] private float oxygenDetectionRadius = 2f;
        [SerializeField] [BoxGroup("PickUpOxygen")] private float oxygenMagneticStartForce = 9f;
        [SerializeField] [BoxGroup("PickUpOxygen")] private float oxygenMagneticEndForce = 2f;
        [SerializeField] [BoxGroup("Feedbacks")] private MMF_Player explodeFeedback;
        [SerializeField] [BoxGroup("Feedbacks")] private MMF_Player mergeFeedback;
        [BoxGroup("Events")] [PropertyOrder(100f)] public UnityEvent onPickUpScore;
        private readonly List<CloningCharacter> _clones = new List<CloningCharacter>();
        private GameObject _cloningParent;
        #endregion -------------------------------------------------------------------------------------------------------------
        
        #region Properties 
        public static float HitCombo;
        public static Player Instance { get; private set; }
        #endregion -------------------------------------------------------------------------------------------------------------
        
        #region UnityMethods 
        protected override void Awake()
        {
            if (!Instance)
                Instance = this;
            onDead.AddListener(() => HitCombo = 0f);
            base.Awake();
        }
        
        protected override void Update()
        {
            base.Update();
            MovementController();
            PickAndPullOxygen();
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
            onPickUpScore?.Invoke();
            Destroy(other.gameObject);
        }
        
        private IEnumerator CloningFollowAndMergedBack(float time)
        {
            float timeCounter = 0;
            while (timeCounter < time)
            {
                _cloningParent.transform.position = Vector2.Lerp(_cloningParent.transform.position, transform.position, timeCounter / time);
                timeCounter += Time.deltaTime;
                yield return null;
            }

            mergeFeedback?.PlayFeedbacks();
            foreach (CloningCharacter clone in _clones)
            {
                yield return new WaitForSeconds(0.05f);
                clone.transform.DOMove(transform.position, 0.25f).SetEase(Ease.InOutSine).OnComplete(() =>
                {
                    AddScore(clone.Score);
                    Destroy(clone.gameObject,0.02f);
                });
            }
            yield return new WaitForSeconds(0.8f);
        }
        #endregion ---------------------------------------------------------------------------------------------

        #region Methods
        private void MovementController()
        {
            if (IsModifyingMovement) return;
            Vector2 mouseDirection = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
            Rigid2D.AddForce(mouseDirection.normalized * CurrentSpeed);
            Rigid2D.velocity = Vector2.ClampMagnitude(Rigid2D.velocity, CurrentSpeed);
        }
        
        private void PickAndPullOxygen()
        {
            Collider2D[] colliders = Array.Empty<Collider2D>();
            int oxygenCount = Physics2D.OverlapCircleNonAlloc(transform.position, (transform.localScale.x / 2) + oxygenDetectionRadius, colliders , LayerMask.GetMask("Oxygen"));
            if (oxygenCount <= 0 )return;
            
            foreach (Collider2D col in colliders)
            {
                Vector2 direction = (transform.position - col.transform.position).normalized;
                Vector2 perpendicularRight = new Vector2(direction.y, -direction.x).normalized;
                Vector2 combinedVector = (direction + perpendicularRight).normalized;
                float force = oxygenMagneticStartForce - (Time.deltaTime*3);
                force = Mathf.Clamp(force, oxygenMagneticEndForce, oxygenMagneticStartForce);
                col.transform.position += (Vector3)(combinedVector * force * Time.deltaTime);
            }
        }

        protected override void SkillInputHandler()
        {
            if (Time.timeScale == 0) return;
            if (Input.GetMouseButton(0))
                skillLeft.UseSkill();
            
            if (Input.GetMouseButton(1))
                skillRight.UseSkill();
        }
        
        public void ExplodeOut8Direction(float force, float mergeTime)
        {
            Vector2[] directions = new Vector2[8];
            if (!_cloningParent)
                _cloningParent = new GameObject("CloningParent");
            _cloningParent.transform.position = transform.position;
            explodeFeedback?.PlayFeedbacks();
            _clones.Clear();
            
            for (int i = 0; i < 8; i++)
            {
                directions[i] = new Vector2(Mathf.Cos(i * Mathf.PI / 4), Mathf.Sin(i * Mathf.PI / 4));
                Vector2 position = _cloningParent.transform.position + ((Vector3)directions[i] * (transform.localScale.x + force));
                Vector2 position2 = _cloningParent.transform.position + ((Vector3)directions[i] * (transform.localScale.x + (force + 2)));
                GameObject obj = Instantiate(gameObject, _cloningParent.transform.position, Quaternion.identity, _cloningParent.transform);
                MonoBehaviour[] scripts = obj.GetComponents<MonoBehaviour>();
                foreach (MonoBehaviour script in scripts) 
                    Destroy(script);
                
                obj.AddComponent(typeof(CloningCharacter));
                CloningCharacter clone = obj.GetComponent<CloningCharacter>();
                NavMeshAgent agent = obj.GetComponent<NavMeshAgent>();
                if (agent) agent.enabled = false;
                _clones.Add(clone);
                clone.OwnerCharacter = this;
                clone.IsIframe = true;
                clone.canApplyDamage = true;
                clone.SetScore(0);
                clone.transform.DOMove(position, 0.25f).SetEase(Ease.InOutSine).OnComplete(() =>
                {
                    clone.transform.DOMove(position2, mergeTime).SetEase(Ease.InOutSine);
                });
            }
            
            StartCoroutine(CloningFollowAndMergedBack(mergeTime));
        }
        
        public void ResizeCamera()
        {
            float size = CameraManager.Instance.StartOrthographicSize;
            CameraManager.Instance.SetLensOrthographicSize(size,0.3f);
        }
        
        public override void Dead(CharacterBase killer, bool dropOxygen = true)
        {
            if (IsDash) return;
            base.Dead(killer, false);
        }
        #endregion ---------------------------------------------------------------------------------------------
    }
}
