using Characters.Controllers;
using Characters.SkillSystems.SkillS;
using UnityEngine;

namespace Characters.SkillSystems
{
    public class SkillSystem : MonoBehaviour
    {
        [SerializeField] private BaseSkill primarySkill;
        [SerializeField] private BaseSkill secondarySkill;
        private float _primarySkillCooldown;
        private float _secondarySkillCooldown;
        private BaseController _owner;

        private void Update()
        {
            ModifyPrimarySkillCooldown(_primarySkillCooldown - Time.deltaTime);
            ModifySecondarySkillCooldown(_secondarySkillCooldown - Time.deltaTime);
        }

        public void Initialize(BaseController owner)
        {
            _owner = owner;
            SetPrimarySkill(primarySkill);
            SetSecondarySkill(secondarySkill);
        }
        
        public void PerformPrimarySkill(Vector2 direction)
        {
            if (!primarySkill) return;
            if (_primarySkillCooldown > 0) return;
            primarySkill.PerformSkill(_owner, direction);
            ModifyPrimarySkillCooldown(primarySkill.cooldownDuration);
        }

        public void PerformSecondarySkill(Vector2 direction)
        {
            if (!secondarySkill) return;
            if (_secondarySkillCooldown > 0) return;
            secondarySkill.PerformSkill(_owner, direction);
            ModifySecondarySkillCooldown(secondarySkill.cooldownDuration);
        }
        
        public void ModifyPrimarySkillCooldown(float value)
        {
            if (!primarySkill) return;
            _primarySkillCooldown = 
                Mathf.Clamp(value, 0, primarySkill.cooldownDuration);
        }

        public void ModifySecondarySkillCooldown(float value)
        {
            if (!secondarySkill) return;
            _secondarySkillCooldown =
                Mathf.Clamp(value, 0, secondarySkill.cooldownDuration);
        }

        public void ResetPrimarySkillCooldown()
        {
            _primarySkillCooldown = 0;
        }

        public void ResetSecondarySkillCooldown()
        {
            _secondarySkillCooldown = 0;
        }

        public void SetPrimarySkill(BaseSkill newSkill)
        {
            primarySkill = newSkill;
            ResetPrimarySkillCooldown();
        }

        public void SetSecondarySkill(BaseSkill newSkill)
        {
            secondarySkill = newSkill;
            ResetSecondarySkillCooldown();
        }
    }
}
