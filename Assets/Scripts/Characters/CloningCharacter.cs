using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace Characters
{
    public class CloningCharacter : CharacterBase
    {
        public enum LifeTimeType
        {
            Destroy,
            MergeBack
        }
        private CharacterBase _ownerCharacter;
        private float _lifeTime;
        private LifeTimeType _endType;
        public bool canDealDamageOnTouch = false;
        public bool destroyAfterTouch = false;
        public CharacterBase OwnerCharacter => _ownerCharacter;
        private const float MergeStartForce = 20f;
        private const float MergeEndForce = 5f;
        private const float MergeForceDecreasePerSec = 3f;

        protected override void Start()
        {
            StartCoroutine(LifeTimeHandler());
            base.Start();
        }

        public void Initialize(CharacterBase owner, float lifeTime, LifeTimeType endType, int life)
        {
            _ownerCharacter = owner;
            _lifeTime = lifeTime;
            _endType = endType;
            this.life = life;
        }
            
        protected override void SkillInputHandler() {}
        
        protected void OnTriggerStay2D(Collider2D other)
        {
            if (!canDealDamageOnTouch) return;
            if (other.CompareTag("Enemy"))
            {
                EnemyManager enemy =  other.GetComponent<EnemyManager>();
                enemy.Dead(this);
                if (destroyAfterTouch) 
                    Dead(enemy);
            }
            
            if (!other.CompareTag("Oxygen")) return;
            Oxygen exp = other.GetComponent<Oxygen>();
            if (!exp.canPickUp) return;
            AddScore(exp.scoreAmount);
            Destroy(other.gameObject);
        }
        
        private IEnumerator LifeTimeHandler()
        {
            yield return new WaitForSeconds(_lifeTime);
            if (IsDead) yield break;
            
            switch (_endType)
            {
                case LifeTimeType.Destroy:
                    Destroy(gameObject);
                    break;
                case LifeTimeType.MergeBack:
                    while (Vector2.Distance(transform.position, _ownerCharacter.transform.position) >= 0.5f && !IsDead)
                    {
                        Vector2 direction = (_ownerCharacter.transform.position-transform.position).normalized;
                        Vector2 perpendicularRight = new Vector2(direction.y, -direction.x).normalized;
                        Vector2 combinedVector = (direction + perpendicularRight).normalized;
                        float force = MergeStartForce - Time.deltaTime * MergeForceDecreasePerSec;
                        force = Mathf.Clamp(force, MergeEndForce, MergeStartForce);
                        transform.position += (Vector3)(combinedVector * (force * Time.deltaTime));
                        yield return null;
                    }
                    OwnerCharacter.AddScore(score);
                    Destroy(gameObject);
                    break;
                default:
                    Destroy(gameObject);
                    break;
            }
        }
    }
}
