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
                float size = CameraManager.Instance.currentCamera.m_Lens.OrthographicSize * cameraPanOutMultiplier;
                CameraManager.Instance.SetLensOrthographicSize(size, 0.25f);
                Player.Instance.IsIframe = true;
            });
            onSkillEnd.AddListener(() =>
            {
                Player.Instance.IsIframe = false;
                Player.Instance.ResizeCamera();
            });
        }
        
        protected override void SkillAction()
        {
            if (!IsPlayer) return;
            Player.Instance.ExplodeOut8Direction(explosionForce,MergeTime);
        }
    }
}
