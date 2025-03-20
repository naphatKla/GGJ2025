using System.Collections;
using Characters.Controllers;
using UnityEngine;

namespace Characters.SkillSystems.SkillS
{
    public abstract class BaseSkill : MonoBehaviour
    {
        [SerializeField] private float cooldownDuration;
        
        public void PerformSkill(BaseController owner, Vector2 direction)
        {
            OnSkillStart(owner, direction);
            StartCoroutine(OnSkillUpdate(owner, direction));
        }
        
        protected abstract void OnSkillStart(BaseController owner, Vector2 direction);
        protected abstract IEnumerator OnSkillUpdate(BaseController owner, Vector2 direction);
    }
}
