using System;
using System.Collections.Generic;
using Player;
using ProjectExtensions;

namespace UI.DotNotify
{
    public class RedDotService : AutoCreatePersistentSingleton<RedDotService>
    {
        public static event Action OnChanged;

        private PlayerData P =>
            ActiveProfileService.Instance?.CurrentProfile ?? ActiveProfileService.Instance?.LoadCurrent();

        private HashSet<string> Keys
        {
            get
            {
                P.RedDotKeys ??= new HashSet<string>();
                return P.RedDotKeys;
            }
        }

        // ---------- Query ----------
        public bool Has(string key)
        {
            return Keys.Contains(key);
        }

        public bool HasPrefix(string prefix)
        {
            foreach (var k in Keys)
                if (k.StartsWith(prefix))
                    return true;
            return false;
        }

        // ---------- Modify ----------
        public void Add(string key, bool saveNow = true)
        {
            if (string.IsNullOrEmpty(key)) return;

            if (Keys.Add(key))
                Commit(saveNow);
        }

        public void Remove(string key, bool saveNow = true)
        {
            if (Keys.Remove(key))
                Commit(saveNow);
        }

        public void RemovePrefix(string prefix, bool saveNow = true)
        {
            var changed = false;

            Keys.RemoveWhere(k =>
            {
                if (!k.StartsWith(prefix)) return false;
                changed = true;
                return true;
            });

            if (changed)
                Commit(saveNow);
        }

        private void Commit(bool saveNow)
        {
            if (saveNow) ActiveProfileService.Instance.SaveNow();
            OnChanged?.Invoke();
        }
    }
}