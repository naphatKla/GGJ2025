using System;
using System.Collections;
using UnityEngine;

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
        public bool canApplyDamageOnTouch = false;
        public CharacterBase OwnerCharacter => _ownerCharacter;

        protected override void Start()
        {
            StartCoroutine(LifeTimeStart());
            base.Start();
        }

        public void Initialize(CharacterBase owner, float lifeTime, LifeTimeType endType)
        {
            _ownerCharacter = owner;
            _lifeTime = lifeTime;
            _endType = endType;
        }
            
        protected override void SkillInputHandler() {}
        /*protected override void OnTriggerStay2D(Collider2D other)
        {
            if (!canApplyDamageOnTouch) return;
            if (other.CompareTag("Enemy"))
            {
                other.GetComponent<EnemyManager>().Dead(this);
            }
            
            if (!other.CompareTag("Oxygen")) return;
            Oxygen exp = other.GetComponent<Oxygen>();
            if (!exp.canPickUp) return;
            AddScore(exp.scoreAmount);
            Destroy(other.gameObject);
        }*/

        private void EndLifeTime()
        {
            switch (_endType)
            {
                case LifeTimeType.Destroy:
                    Destroy(gameObject);
                    break;
                case LifeTimeType.MergeBack:
                    
                    break;
                default:
                    Destroy(gameObject);
                    break;
            }    
        }
        
        private IEnumerator LifeTimeStart()
        {
            yield return new WaitForSeconds(_lifeTime);
            
        }
    }
}
