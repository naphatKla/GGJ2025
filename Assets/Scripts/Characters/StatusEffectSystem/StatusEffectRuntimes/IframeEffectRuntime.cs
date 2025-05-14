using UnityEngine;

namespace Characters.StatusEffectSystem.StatusEffectRuntimes
{
    public class IframeEffectRuntime : BaseStatusEffectRuntime
    {
        [SerializeField] private float testForDash1;
        [SerializeField] private float testForDash2;
        
        public override void OnStart(GameObject owner)
        {
            // Set iframe flag, play effect, etc.
        }

        public override void OnTick(GameObject owner, float deltaTime)
        {
        }

        public override void OnEnd(GameObject owner)
        {
            // Reset iframe flag
        }
    }
}
