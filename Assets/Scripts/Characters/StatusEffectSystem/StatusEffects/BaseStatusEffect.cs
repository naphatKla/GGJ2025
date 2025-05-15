using UnityEngine;

namespace Characters.StatusEffectSystem.StatusEffects
{
    public abstract class BaseStatusEffect
    {
        [SerializeField] private bool isOverrideDuration;
        [SerializeField] private float currentDuration;

        public bool IsOverrideDuration => isOverrideDuration;
        public float CurrentDuration => currentDuration;
        public bool IsDone => currentDuration <= 0f;


        protected abstract void OnStart(GameObject owner);
        protected abstract void OnUpdate(GameObject owner, float deltaTime);
        protected abstract void OnExit(GameObject owner);

        public void HandleStart(GameObject owner)
        {
            OnStart(owner);
        }

        public void HandleUpdate(GameObject owner, float deltaTime)
        {
            currentDuration -= deltaTime;
            OnUpdate(owner, deltaTime);
        }

        public void HandleExit(GameObject owner)
        {
            OnExit(owner);
        }
        
        public void ClearEffect()
        {
            currentDuration = 0;
        }
    }
}
