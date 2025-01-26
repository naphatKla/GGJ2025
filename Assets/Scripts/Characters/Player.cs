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
        [SerializeField] [BoxGroup("Life")] private int life = 3;
        [SerializeField] [BoxGroup("Life")] private float iframeAfterhitDuration = 1f;
        [SerializeField] [BoxGroup("PickUpOxygen")] private float oxygenDetectionRadius = 2f;
        [SerializeField] [BoxGroup("PickUpOxygen")] private float oxygenMagneticStartForce = 9f;
        [SerializeField] [BoxGroup("PickUpOxygen")] private float oxygenMagneticEndForce = 2f;
        [SerializeField] [BoxGroup("PickUpOxygen")] private float oxygenCurveForce = 5f;
        [SerializeField] [BoxGroup("Feedbacks")] private MMF_Player explodeFeedback;
        [SerializeField] [BoxGroup("Feedbacks")] private MMF_Player mergeFeedback;
        [SerializeField] [BoxGroup("Feedbacks")] private MMF_Player takeDamageFeedback;
        [SerializeField] [BoxGroup("Events")] private UnityEvent onPlayerDead;
        private List<CloningCharacter> _clones = new List<CloningCharacter>();
        private GameObject _cloningParent;
        public static Player Instance { get; private set; }
        public static float Hitcombo;
        
        public UnityEvent onPickUpScore;
        private float _lastHitTime = 0f;

        protected override void Awake()
        {
            if (!Instance)
                Instance = this;
            base.Awake();
        }

        public void ResizeCamera()
        {
            float size = CameraManager.Instance.StartOrthographicSize;
            CameraManager.Instance.SetLensOrthographicSize(size,0.3f);
        }
        
        protected override void Update()
        {
            base.Update();
            MovementController();
            
            // เก็บ oxygen รอบๆรัศมี
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

        protected virtual void OnTriggerStay2D(Collider2D other)
        {
            if (isDead) return;
            if (other.CompareTag("Enemy") && IsDash) 
                other.GetComponent<EnemyManager>().Dead(this);
            
            if (!other.CompareTag("Exp")) return;
            ExpScript exp = other.GetComponent<ExpScript>();
            if (!exp.canPickUp) return;
            AddScore(exp.expAmount);
            onPickUpScore?.Invoke();
            Destroy(other.gameObject);
        }
        
        private void MovementController()
        {
            if (IsModifyingMovement) return;
            Vector2 mouseDirection = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
            rigidbody2D.AddForce(mouseDirection.normalized * CurrentSpeed);
            rigidbody2D.velocity = Vector2.ClampMagnitude(rigidbody2D.velocity, CurrentSpeed);
        }

        protected override void SkillInputHandler()
        {
            if (Time.timeScale == 0) return;
            if (Input.GetMouseButton(0))
                SkillMouseLeft.UseSkill();
            
            if (Input.GetMouseButton(1))
                SkillMouseRight.UseSkill();
        }
        
        [Button]
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
                clone.CanDead = false;
                clone.canApplyDamage = true;
                clone.SetScore(0);
                clone.transform.DOMove(position, 0.25f).SetEase(Ease.InOutSine).OnComplete(() =>
                {
                    clone.transform.DOMove(position2, mergeTime).SetEase(Ease.InOutSine);
                });
            }
            
            StartCoroutine(CloningFollowAndMergedBack(mergeTime));
        }

        IEnumerator CloningFollowAndMergedBack(float time)
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
        
        public override void Dead(CharacterBase killer, bool dropOxygen = true)
        {
            if (Time.time - _lastHitTime < iframeAfterhitDuration) return;
            if (!CanDead) return;
            if (IsDash) return;
            life--;
            _lastHitTime = Time.time;
            takeDamageFeedback?.PlayFeedbacks();
            if (life > 0) return;
            onPlayerDead?.Invoke();
            Hitcombo = 0;
            base.Dead(killer, false);
        }
    }
}
