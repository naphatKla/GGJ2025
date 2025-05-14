using UnityEngine;

namespace Characters.StatusEffectSystem.StatusEffects
{
    public abstract class BaseStatusEffect
    {
        public float currentDuration;
        public bool IsDone => currentDuration <= 0f;

        public abstract void OnStart(GameObject owner);
        public abstract void OnTick(GameObject owner, float deltaTime);
        public abstract void OnEnd(GameObject owner);
    }
}
