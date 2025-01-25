using UnityEngine;

namespace Skills
{
    public class SkillBlackHole : SkillBase
    {
        [SerializeField] protected float explosionForce = 3f;
        [SerializeField] protected float MergeTime = 2f;
        [SerializeField] protected float cameraPanOutMultiplier = 1.5f;
        private float startOrthographicSize;
        
        private void Start()
        {
            onSkillStart.AddListener(() =>
            {
                startOrthographicSize = CameraManager.Instance.currentCamera.m_Lens.OrthographicSize;
                CameraManager.Instance.SetLensOrthographicSize(startOrthographicSize * cameraPanOutMultiplier, 0.25f);
            });
            onSkillEnd.AddListener(() =>
            {
                CameraManager.Instance.SetLensOrthographicSize(startOrthographicSize, 0.25f);
            });
        }
        
        protected override void SkillAction()
        {
            OwnerCharacter.ExplodeOut8Direction(explosionForce,MergeTime);
        }
    }
}
