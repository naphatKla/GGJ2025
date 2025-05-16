using UnityEngine;

namespace Characters.StatusEffectSystem.StatusEffects
{
    public abstract class BaseStatusEffect
    {
        private StatusEffectName _effectName;
        private float _currentDuration;
        private int _level;
        
        public StatusEffectName EffectName => _effectName;

        public float CurrentDuration
        {
            get => _currentDuration;
            set => _currentDuration = value;
        }
        
        public int Level => _level;
        public bool IsDone => _currentDuration <= 0f;
        
        public abstract void OnStart(GameObject owner);
        public abstract void OnUpdate(float deltaTime);
        public abstract void OnExit();

        public void AssignEffectData(StatusEffectName effectName, float duration, int level)
        {
            _effectName = effectName;
            _currentDuration = duration;
            _level = level;
        }

        public void ClearThisEffect() => _currentDuration = 0;
    }
}
