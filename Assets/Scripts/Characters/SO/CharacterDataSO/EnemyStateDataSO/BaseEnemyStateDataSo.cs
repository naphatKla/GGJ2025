using System;
using Characters.Controllers.EnemyStates;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Characters.SO.CharacterDataSO.EnemyStateDataSO
{
    public class BaseEnemyStateDataSo : ScriptableObject
    {
        [Space] [Title("Runtime Binding")]
        [ShowInInspector, OdinSerialize, PropertyOrder(10000)]
        [TypeDrawerSettings(BaseType = typeof(BaseEnemyState))]
        private Type _skillRuntime;
    }
}
