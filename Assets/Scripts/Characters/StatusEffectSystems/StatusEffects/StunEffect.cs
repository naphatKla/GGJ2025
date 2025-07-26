using Characters.Controllers;
using Characters.SO.StatusEffectSO;
using UnityEngine;

namespace Characters.StatusEffectSystems.StatusEffects
{
    public class StunEffect : BaseStatusEffect<StunEffectDataSo>
    {
        private BaseController ownerController;
        public override void OnStart(GameObject owner)
        {
            ownerController = owner.GetComponent<BaseController>();
            ownerController.MovementSystem.SetCanMove(false);
            ownerController.SkillSystem.SetCanUseSkills(false);
        }

        public override void OnUpdate(float deltaTime)
        {
            
        }

        public override void OnExit()
        {
            ownerController.MovementSystem.SetCanMove(true);
            ownerController.SkillSystem.SetCanUseSkills(true);
            ownerController = null;
        }
    }
}
