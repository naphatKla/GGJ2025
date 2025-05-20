using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace GlobalSettings
{
    public class CharacterGlobalSettings : GlobalSetting<CharacterGlobalSettings>
    {
        #region Inspector & Fields 
        
        [Title("LayerMask")]
        [DictionaryDrawerSettings(KeyLabel = "Tag", ValueLabel = "Enemy Layer")] [OdinSerialize]
        private Dictionary<string, LayerMask> _enemyLayerDictionary = new Dictionary<string, LayerMask>();

        [Space]
        [SerializeField] private LayerMask levelBoundLayerMask;

        [Space] 
        [SerializeField] private LayerMask collectableItemLayerMask;
        
        #endregion -------------------------------------------------------------------------------------

        #region Properties
        public Dictionary<string, LayerMask> EnemyLayerDictionary => _enemyLayerDictionary;
        public LayerMask LevelBoundLayerMask => levelBoundLayerMask;
        public LayerMask CollectableItemLayerMask => collectableItemLayerMask;

        #endregion
    }
}
