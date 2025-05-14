using Characters.StatusEffectSystem;
using UnityEngine;

namespace Characters.SO.StatusEffectMetaSO
{
    [CreateAssetMenu(fileName = "StatusEffectMetaData", menuName = "GameData/StatusEffectMetaData")]
    public class StatusEffectMetaDataSo : ScriptableObject
    {
        public StatusEffectName effectName;
        public string displayName;
        public Sprite icon;
        [TextArea] public string description;
        public float defaultDuration;
    }
}
