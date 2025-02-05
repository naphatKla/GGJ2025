using Characters;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Skills
{
    public class SkillBlackHole : SkillBase
    {
        #region Inspectors & Fields
        [Title("BlackHoleSkill")] [SerializeField]
        protected int cloningAmount = 8;

        [SerializeField] [BoxGroup("Duration")]
        protected float mergeTime = 2f;

        [SerializeField] protected float explosionForce = 3f;
        [SerializeField] protected bool iframeOnPerformingSkill;
        [SerializeField] protected bool cloningIframeOnPerformingSkill;
        [SerializeField] protected bool cloningDealDamageOnTouch;
        [SerializeField] protected bool cloningDestroyAfterTouch;
        [Title("PlayerOnly")] [SerializeField] protected float cameraPanOutMultiplier = 1.5f;
        #endregion -------------------------------------------------------------------------------------------------------------------

        #region Methods
        protected override void OnSkillStart()
        {
            OwnerCharacter.IsIframe = iframeOnPerformingSkill;
            Vector2[] directions = new Vector2[cloningAmount];
            for (int i = 0; i < cloningAmount; i++)
            {
                float angle = i * 2 * Mathf.PI / cloningAmount;
                directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 explodePos = transform.position + ((Vector3)directions[i] * (transform.localScale.x + explosionForce));
                CloningCharacter clone = OwnerCharacter.CreateCloning(mergeTime,
                    CloningCharacter.LifeTimeType.MergeBack, 1, cloningDealDamageOnTouch, cloningDestroyAfterTouch);
                clone.IsIframe = cloningIframeOnPerformingSkill;
                clone.Animator.Play("BackHole");
                clone.transform.DOMove(explodePos, 0.25f).SetEase(Ease.InOutSine).OnComplete(() =>
                {
                    clone.transform.DOMove(explodePos * 1.15f, mergeTime).SetEase(Ease.InOutSine);
                });
            }
            
            if (!IsPlayer) return;
            float size = CameraManager.Instance.currentCamera.m_Lens.OrthographicSize * cameraPanOutMultiplier;
            CameraManager.Instance.SetLensOrthographicSize(size, 0.25f);
        }
        
        protected override void OnSkillEnd()
        {
            OwnerCharacter.IsIframe = false;
            if (!IsPlayer) return;
            CameraManager.Instance.ResetLensOrthographicSize();
        }
        #endregion -------------------------------------------------------------------------------------------------------------------
    }
}
