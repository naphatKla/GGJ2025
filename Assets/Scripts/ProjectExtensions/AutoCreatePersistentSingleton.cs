using UnityEngine;

namespace ProjectExtensions
{
    /// <summary>
    /// Singleton ข้ามซีน (DDOL) แบบ "auto-create"
    /// - ถ้าไม่พบ: สร้างใหม่และ DontDestroyOnLoad
    /// - (ออปชัน) จัดเก็บไว้ใต้โหนดราก DDOL เพื่อความเป็นระเบียบ
    /// </summary>
    public abstract class AutoCreatePersistentSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static bool _isCreating;
        private static Transform _ddolRoot;

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
                if (_instance != null) return _instance;

                return CreateNewInstance();
            }
        }

        private static Transform GetOrCreateRoot()
        {
            if (_ddolRoot != null) return _ddolRoot;

            var rootGo = GameObject.Find("/___Singletons(DDOL)");
            if (rootGo == null)
            {
                rootGo = new GameObject("___Singletons(DDOL)");
                Object.DontDestroyOnLoad(rootGo);
            }
            _ddolRoot = rootGo.transform;
            return _ddolRoot;
        }

        private static T CreateNewInstance()
        {
            if (_isCreating) return null;

            _isCreating = true;
            try
            {
                SingletonDiagnostics.LogAutoCreate<T>("no existing instance found");
                var go = new GameObject($"[{typeof(T).Name}] (AutoCreated)");
                go.transform.SetParent(GetOrCreateRoot(), worldPositionStays: false);
                Object.DontDestroyOnLoad(go);
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
            var root = GetOrCreateRoot();
            transform.SetParent(root, worldPositionStays: false);
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
