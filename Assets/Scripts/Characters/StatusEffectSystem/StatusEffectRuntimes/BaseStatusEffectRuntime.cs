using UnityEngine;

namespace Characters.StatusEffectSystem.StatusEffectRuntimes
{
    public abstract class BaseStatusEffectRuntime : MonoBehaviour
    {
        public float duration;
        public StatusEffectName EffectName { get; protected set; }

        public abstract void OnStart(GameObject owner);
        public abstract void OnTick(GameObject owner, float deltaTime);
        public abstract void OnEnd(GameObject owner);

        public bool IsDone => duration <= 0f;
    }
}
