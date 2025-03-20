using System.Collections;
using Characters.Controllers;
using UnityEngine;

namespace Characters.SkillSystems.SkillS
{
    public abstract class BaseSkill : MonoBehaviour
    {
        [SerializeField] private float cooldownDuration;
        
        public void PerformSkill(BaseController owner)
        {
            OnSkillStart(owner);
            StartCoroutine(OnSkillUpdate(owner));
        }
        
        protected abstract void OnSkillStart(BaseController owner);
        protected abstract IEnumerator OnSkillUpdate(BaseController owner);
    }
}
