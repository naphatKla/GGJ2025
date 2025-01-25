using Characters;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Skills
{
    public class SkillBlackHole : SkillBase
    {
        [Title("BlackHoleSkill")] [SerializeField] protected float explosionForce = 3f;
        [SerializeField] protected float MergeTime = 2f;
        [SerializeField] protected float cameraPanOutMultiplier = 1.5f;
        
        private void Start()
        {
            if (!IsPlayer) return;
            onSkillStart.AddListener(() =>
            {
                CameraManager.Instance.SetLensOrthographicSize(CameraManager.Instance.currentCamera.m_Lens.OrthographicSize * cameraPanOutMultiplier, 0.25f);
            });
            onSkillEnd.AddListener(() =>
            {
                OwnerCharacter.GetComponent<Player>().ResizeCamera();
            });
        }
        
        protected override void SkillAction()
        {
            OwnerCharacter.ExplodeOut8Direction(explosionForce,MergeTime);
        }
    }
}
