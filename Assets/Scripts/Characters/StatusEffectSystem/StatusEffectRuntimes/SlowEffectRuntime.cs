using UnityEngine;

namespace Characters.StatusEffectSystem.StatusEffectRuntimes
{
    public class SlowEffectRuntime : BaseStatusEffectRuntime
    {
        [SerializeField] private float testForSlow1;
        [SerializeField] private float testForSlow2;
        
        
        public override void OnStart(GameObject owner)
        {

        }

        public override void OnTick(GameObject owner, float deltaTime)
        {
      
        }

        public override void OnEnd(GameObject owner)
        {
           
        }
    }
}
