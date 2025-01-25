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
                Debug.LogWarning("KILL");
                other.GetComponent<EnemyManager>().Dead(this);
            }
            
            if (!other.CompareTag("Exp")) return;
            ExpScript exp = other.GetComponent<ExpScript>();
            if (!exp.canPickUp) return;
            AddScore(exp.expAmount);
            Destroy(other.gameObject);
        }
    }
}
