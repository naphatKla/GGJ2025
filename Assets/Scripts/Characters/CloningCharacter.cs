using System;
using UnityEngine;

namespace Characters
{
    public class CloningCharacter : Player
    {
        public CharacterBase OwnerCharacter;
        public bool canApplyDamage = false;
        protected override void SkillInputHandler() { }

        protected override void OnTriggerStay2D(Collider2D other)
        {
            if (!canApplyDamage) return;
            if (other.CompareTag("Enemy"))
            {
                other.GetComponent<EnemyManager>().Dead(this);
            }
            
            if (!other.CompareTag("Oxygen")) return;
            Oxygen exp = other.GetComponent<Oxygen>();
            if (!exp.canPickUp) return;
            AddScore(exp.scoreAmount);
            Destroy(other.gameObject);
        }
    }
}
