using System.Collections;
using UnityEngine;

namespace Characters
{
    public class CloningCharacter : CharacterBase
    {
        #region #region Inspectors & Fields
        private CharacterBase _ownerCharacter;
        private float _lifeTime;
        private LifeTimeType _endType;
        private bool _canDealDamageOnTouch;
        private bool _destroyAfterTouch;
        private float _mergeStartForce = 60f;
        private float _mergeEndForce = 20f;
        private float _mergeForceDecayRate = 3f;

        public enum LifeTimeType
        {
            Destroy,
            MergeBack
        }
        #endregion -------------------------------------------------------------------------------------------------------------------

        #region Properties
        public CharacterBase OwnerCharacter => _ownerCharacter;
        #endregion -------------------------------------------------------------------------------------------------------------------

        #region UnityMethods
        protected void Start()
        {
            StartCoroutine(LifeTimeHandler());
        }

        protected override void OnTriggerStay2D(Collider2D other)
        {
            base.OnTriggerStay2D(other);
            other.TryGetComponent(out CharacterBase target);
            if (!target || target.CompareTag(tag)) return;
            if (_destroyAfterTouch) Dead(target);
            if (!_canDealDamageOnTouch) return;
            target.TakeDamage(this);
        }

        private IEnumerator LifeTimeHandler()
        {
            yield return new WaitForSeconds(_lifeTime);
            if (IsDead) yield break;

            switch (_endType)
            {
                case LifeTimeType.Destroy:
                    Dead(null);
                    break;
                case LifeTimeType.MergeBack:
                    while (_ownerCharacter &&
                           Vector2.Distance(transform.position, _ownerCharacter.transform.position) >= 0.5f && !IsDead)
                    {
                        Vector2 direction = (_ownerCharacter.transform.position - transform.position).normalized;
                        Vector2 perpendicularRight = new Vector2(direction.y, -direction.x).normalized;
                        Vector2 combinedVector = (direction + perpendicularRight).normalized;
                        float force = _mergeStartForce - Time.deltaTime * _mergeForceDecayRate;
                        force = Mathf.Clamp(force, _mergeEndForce, _mergeStartForce);
                        transform.position += (Vector3)(combinedVector * (force * Time.deltaTime));
                        yield return null;
                    }

                    OwnerCharacter.AddScore(score);
                    Destroy(gameObject);
                    break;
                default:
                    Dead(null);
                    break;
            }
        }
        #endregion -------------------------------------------------------------------------------------------------------------------

        #region Methods
        protected override void SkillInputHandler()
        {
        }

        public void Initialize(CharacterBase owner, int characterLife, float lifeTime, LifeTimeType endType,
            bool dealDamageOnTouch, bool destroyOnTouch, bool canPickUpOxygen)
        {
            _ownerCharacter = owner;
            _lifeTime = lifeTime;
            _endType = endType;
            life = characterLife;
            _canDealDamageOnTouch = dealDamageOnTouch;
            _destroyAfterTouch = destroyOnTouch;
            canCollectOxygen = canPickUpOxygen;
        }

        public void SetMergeForce(float startForce, float endForce, float decayRate)
        {
            _mergeStartForce = startForce;
            _mergeEndForce = endForce;
            _mergeForceDecayRate = decayRate;
        }
        #endregion
    }
}