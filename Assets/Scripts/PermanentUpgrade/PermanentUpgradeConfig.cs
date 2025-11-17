using System.Collections.Generic;
using UnityEngine;

namespace PermanentUpgrade
{
    [CreateAssetMenu(menuName = "Permanent Upgrade Config")]
    public class PermanentUpgradeConfig : ScriptableObject
    {
        [SerializeField]
        private List<PermanentUpgradeEntry> entries = new List<PermanentUpgradeEntry>();

        private Dictionary<PermanentUpgradeType, PermanentUpgradeEntry> _cache;

        void OnEnable()
        {
            BuildCache();
        }

        private void BuildCache()
        {
            _cache = new Dictionary<PermanentUpgradeType, PermanentUpgradeEntry>();
            foreach (var e in entries)
            {
                if (e == null) continue;
                _cache[e.type] = e;
            }
        }

        public PermanentUpgradeEntry GetEntry(PermanentUpgradeType type)
        {
            if (_cache == null || _cache.Count == 0)
            {
                BuildCache();
            }
            return _cache != null && _cache.TryGetValue(type, out var e) ? e : null;
        }
    }
}