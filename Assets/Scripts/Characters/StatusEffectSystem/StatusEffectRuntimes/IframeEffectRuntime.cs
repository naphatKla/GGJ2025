using UnityEngine;

namespace Characters.StatusEffectSystem.StatusEffectRuntimes
{
    public class IframeEffectRuntime : BaseStatusEffectRuntime
    {
        public IframeEffectRuntime(float duration)
        {
            EffectName = StatusEffectName.Iframe;
            this.duration = duration;
        }

        public override void OnStart(GameObject owner)
        {
            // Set iframe flag, play effect, etc.
        }

        public override void OnTick(GameObject owner, float deltaTime)
        {
            duration -= deltaTime;
        }

        public override void OnEnd(GameObject owner)
        {
            // Reset iframe flag
        }
    }
}
