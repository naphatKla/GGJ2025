using UnityEngine;

namespace ProjectExtensions
{
    /// <summary>
    /// Singleton ข้ามซีน (DDOL) แบบ "ไม่ auto-create"
    /// </summary>
    public abstract class NonAutoCreatePersistentSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Current => _instance;
        public static bool IsAlive => _instance != null;

        public static T Instance
        {
            get
            {
                if (SingletonDiagnostics.IsInTearDownStack())
                    SingletonDiagnostics.LogTeardownAccess<T>();

                if (_instance != null) return _instance;
                _instance = FindObjectOfType<T>(includeInactive: true);
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                SingletonDiagnostics.LogDuplicate<T>(this);
                Destroy(gameObject);
                return;
            }

            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}