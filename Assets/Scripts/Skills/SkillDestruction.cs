using System.Collections;
using Characters;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Skills
{
    public class SkillDestruction : SkillBase
    {
        #region Inspectors & Fields

        [Title("SkillDash")] [SerializeField] private ParticleSystem bombParticle;
        [SerializeField] private float bombDistance;
        [SerializeField] private float bombTime;
        [SerializeField] private bool deadAfterBomb = true;

        #endregion -------------------------------------------------------------------------------------------------------------------

        #region Methods

        protected override void OnSkillStart()
        {
            StartCoroutine(Destruction());
        }
        
        protected override void OnSkillEnd()
        {
            OwnerCharacter.StartMovementController();
            OwnerCharacter.IsDash = false;
            if (!deadAfterBomb) return;
            OwnerCharacter.ForceDead(OwnerCharacter);
        }
        
        private IEnumerator Destruction()
        {
            OwnerCharacter.StopMovementController();
            bombParticle.Play();

            float timer = 0f;
            while (timer < bombTime)
            {
                if (!OwnerCharacter)
                {
                    ExitSkill();
                    yield break;
                }

                timer += Time.deltaTime;
                yield return null;
            }

            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(OwnerCharacter.transform.position, bombDistance,
                OwnerCharacter.EnemyLayerMask);
            foreach (Collider2D hit in hitColliders)
            {
                if (hit.TryGetComponent(out CharacterBase target))
                    target.TakeDamage(OwnerCharacter);
            }

            ExitSkill();
        }

        private void OnDrawGizmos()
        {
            if (Application.IsPlaying(this)) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, bombDistance);
        }

        #endregion -------------------------------------------------------------------------------------------------------------------
    }
}