using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectExtensions
{
    /// <summary>
    /// Singleton แบบ "auto-create" สำหรับออบเจ็กต์ที่ต้องอยู่ข้ามซีน (DontDestroyOnLoad)
    /// - Instance จะ auto-create เมื่อถูกเรียกครั้งแรก (เฉพาะเวลาปลอดภัย)
    /// - ระหว่างกำลังออกเกม/เปลี่ยนซีน จะไม่สร้าง (คืน null)
    /// - มี Current/IsAlive สำหรับเช็คโดยไม่ก่อให้เกิดการสร้าง
    /// - ป้องกัน duplicate
    /// - จัดเก็บไว้ใต้โหนดราก DDOL เดียวเพื่อความเรียบร้อย
    /// </summary>
    public abstract class AutoCreatePersistentSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static bool _isQuitting;
        private static bool _isSceneChanging;
        private static bool _isCreating;
        private static Transform _ddolRoot;

        public static T Current => _instance;
        public static bool IsAlive => _instance != null && !_isQuitting;

        public static T Instance
        {
            get
            {
                if (_isQuitting || _isSceneChanging) return null;
                if (_instance != null) return _instance;

                // หาใน DDOL scene ก่อน (รวมถึง inactive)
                _instance = FindObjectOfType<T>(includeInactive: true);
                if (_instance != null) return _instance;

                return CreateNewInstance();
            }
        }

        private static Transform GetOrCreateRoot()
        {
            if (_ddolRoot != null) return _ddolRoot;

            var root = GameObject.Find("/___Singletons(DDontDestroyOnLoad)")?.transform;
            if (root == null)
            {
                var go = new GameObject("___Singletons(DDontDestroyOnLoad)");
                DontDestroyOnLoad(go);
                root = go.transform;
            }
            _ddolRoot = root;
            return _ddolRoot;
        }

        private static T CreateNewInstance()
        {
            if (_isQuitting || _isSceneChanging || _isCreating) return null;

            _isCreating = true;
            try
            {
                var go = new GameObject($"[{typeof(T).Name}] (AutoCreated)");
                go.transform.SetParent(GetOrCreateRoot(), worldPositionStays: false);
                var inst = go.AddComponent<T>();
                // Awake จะตั้ง _instance และ DDOL ให้แล้ว แต่กันไว้ด้วย:
                DontDestroyOnLoad(go);
                return inst;
            }
            finally
            {
                _isCreating = false;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            _isQuitting = false;
            _isSceneChanging = false;
            _isCreating = false;
            _ddolRoot = null;

            SceneManager.sceneUnloaded -= OnAnySceneUnloaded;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            SceneManager.sceneUnloaded += OnAnySceneUnloaded;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;

            Application.quitting -= OnAppQuitting;
            Application.quitting += OnAppQuitting;
        }

        private static void OnAnySceneUnloaded(Scene s)               => _isSceneChanging = true;
        private static void OnActiveSceneChanged(Scene a, Scene b)    => _isSceneChanging = false;
        private static void OnAppQuitting()                           => _isQuitting = true;

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[AutoCreatePersistentSingleton<{typeof(T).Name}>] Duplicate detected, destroying {name}.");
                Destroy(gameObject);
                return;
            }

            _instance = this as T;
            // เก็บไว้ในราก DDOL ที่กำหนด
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
