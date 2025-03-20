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

        public void Initialize(BaseController owner)
        {
            _owner = owner;
        }
        
        public void PerformPrimarySkill()
        {
            primarySkill.PerformSkill(_owner);
        }
        
        
        public void ResetCooldown(bool primary = true, bool secondary = true)
        {
            if (primary) _primarySkillCooldown = 0;
            if (secondary) _secondarySkillCooldown = 0;
        }
    }
}
