using UnityEngine;

namespace Characters.StatusEffectSystem.StatusEffectRuntimes
{
    public abstract class BaseStatusEffectRuntime : MonoBehaviour
    {
        public float currentDuration;
        public bool IsDone => currentDuration <= 0f;

        public abstract void OnStart(GameObject owner);
        public abstract void OnTick(GameObject owner, float deltaTime);
        public abstract void OnEnd(GameObject owner);
    }
}
