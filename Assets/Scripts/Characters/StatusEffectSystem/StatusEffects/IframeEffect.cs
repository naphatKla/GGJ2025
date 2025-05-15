using UnityEngine;
using UnityEngine.Serialization;

namespace Characters.StatusEffectSystem.StatusEffects
{
    public class IframeEffect : BaseStatusEffect
    {
        [SerializeField] private float sloawParam;
        
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
