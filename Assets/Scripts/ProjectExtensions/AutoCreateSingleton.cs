using UnityEngine;

namespace ProjectExtensions
{
    /// <summary>
    /// Singleton ภายในซีน (ไม่ DDOL) แบบ "auto-create"
    /// - ถ้าไม่พบในซีน: สร้าง GameObject ใหม่และ AddComponent<T>()
    /// - Debug: เตือนถ้า auto-create, เตือนถ้าเรียกจาก teardown, เตือน duplicate
    /// </summary>
    public abstract class AutoCreateSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static bool _isCreating;

        public static T Current => _instance;
        public static bool IsAlive => _instance != null;

        public static T Instance
        {
            get
            {
                if (SingletonDiagnostics.IsInTearDownStack())
                    SingletonDiagnostics.LogTeardownAccess<T>();

                if (_instance != null) return _instance;

                // หาในซีนก่อน (เผื่อ dev วางไว้เอง)
                _instance = FindObjectOfType<T>(includeInactive: true);
                if (_instance != null) return _instance;

                // สร้างใหม่
                return CreateNewInstance();
            }
        }

        private static T CreateNewInstance()
        {
            if (_isCreating) return null;

            _isCreating = true;
            try
            {
                SingletonDiagnostics.LogAutoCreate<T>("no existing instance found");
                var go = new GameObject($"[{typeof(T).Name}] (AutoCreated)");
                var inst = go.AddComponent<T>();
                return inst;
            }
            finally { _isCreating = false; }
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
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
