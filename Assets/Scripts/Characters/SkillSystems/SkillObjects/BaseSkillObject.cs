using Characters.CombatSystems;
using Characters.MovementSystems;
using UnityEngine;

namespace Characters.SkillSystems.SkillObjects
{
    [RequireComponent(typeof(BaseMovementSystem), typeof(DamageOnTouch))]
    public abstract class BaseSkillObject : MonoBehaviour
    {
        private BaseMovementSystem _movementSystem;
        private DamageOnTouch _damageOnTouch;

        public BaseMovementSystem MovementSystem => _movementSystem;
        public DamageOnTouch DamageOnTouch => _damageOnTouch;
        
        protected void Awake()
        {
            _movementSystem = GetComponent<BaseMovementSystem>();
            _damageOnTouch = GetComponent<DamageOnTouch>();
        }
    }
}
