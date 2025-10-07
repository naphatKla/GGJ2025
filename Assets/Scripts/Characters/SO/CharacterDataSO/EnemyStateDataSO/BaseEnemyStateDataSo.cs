using System;
using Characters.Controllers.EnemyStates;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Characters.SO.CharacterDataSO.EnemyStateDataSO
{
    [Serializable]
    public struct EnemyStateDataPayload
    {
        [Required]
        [SerializeField] private BaseEnemyStateDataSo stateData;
        
        [PropertyRange(0, 100)]
        [OnValueChanged(nameof(ClampHP))] 
        [SerializeField] private float hpPercentageToEnter;

        public BaseEnemyStateDataSo StateData => stateData;
        public float HpPercentageToEnter => hpPercentageToEnter;
        
        private void ClampHP()
        {
            hpPercentageToEnter = Mathf.Clamp(hpPercentageToEnter, 0f, 100f);
        }
    }
    
    public class BaseEnemyStateDataSo : SerializedScriptableObject
    {
        [Space] [Title("Runtime Binding")]
        [ShowInInspector, OdinSerialize, PropertyOrder(10000)]
        [TypeDrawerSettings(BaseType = typeof(BaseEnemyState))]
        private Type _skillRuntime;

        public Type SkillRuntime => _skillRuntime;
    }
}
