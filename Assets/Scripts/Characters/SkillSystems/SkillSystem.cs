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

        
        public void ResetCooldown(bool resetPrimary = true, bool resetSecondary = true)
        {
            if (resetPrimary)
                _primarySkillCooldown = 0;
            if (resetSecondary)
                _secondarySkillCooldown = 0;
        }
    }
}
