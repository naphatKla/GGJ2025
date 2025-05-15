using UnityEngine;
using UnityEngine.Serialization;

namespace Characters.StatusEffectSystem.StatusEffects
{
    public class SlowEffect : BaseStatusEffect
    {
        [SerializeField] private float slowParam;
        [SerializeField] private float slowParam2;
        
        
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
