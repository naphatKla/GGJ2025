using System;
using System.Collections;
using Characters;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using VHierarchy.Libs;

namespace Skills
{
    public class SkillDestruction : SkillBase
    {
        #region Inspectors & Fields
        [Title("SkillDash")] 
        [SerializeField] private GameObject bombParticle;
        [SerializeField] private float bombDistance;
        [SerializeField] private float bombTime;
        #endregion -------------------------------------------------------------------------------------------------------------------

        #region Methods

        private void Awake()
        {
            bombParticle.GetComponent<ParticleSystem>().Stop();
        }

        protected override void OnSkillStart()
        {
            StartCoroutine(Destruction());
        }

        private IEnumerator Destruction()
        {
            OwnerCharacter.IsModifyingMovement = true;
            if (!IsPlayer)
            {
                OwnerCharacter.TryGetComponent(out NavMeshAgent agent);
                agent.enabled = false;
            }
            OwnerCharacter.TryGetComponent(out Rigidbody2D rigid2D);
            rigid2D.velocity = Vector2.zero;
            bombParticle.GetComponent<ParticleSystem>().Play();
            var timer = 0f;
            while (timer < bombTime)
            {
                if (OwnerCharacter == null)
                    yield break;
                timer += Time.deltaTime;
                yield return null;
            }
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(OwnerCharacter.transform.position, bombDistance);
            foreach (Collider2D hit in hitColliders)
            {
                if (hit.CompareTag("Player"))
                {
                    hit.GetComponent<CharacterBase>().TakeDamage(OwnerCharacter);
                }
            }
        }

        protected override void OnSkillEnd()
        {
            OwnerCharacter.IsModifyingMovement = false;
            OwnerCharacter.IsDash = false;
            if (IsPlayer) return;
            OwnerCharacter.ForceDead(OwnerCharacter);
        }

        private void OnDrawGizmos()
        {
            if (OwnerCharacter != null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, bombDistance);
        }

        #endregion -------------------------------------------------------------------------------------------------------------------
    }
}
