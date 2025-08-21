using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectExtensions
{
    /// <summary>
    /// Singleton สำหรับออบเจ็กต์ภายในซีน (ไม่ DontDestroyOnLoad)
    /// - Instance จะไม่ auto-create (ถ้าไม่มีจะคืน null)
    /// - Current/IsAlive ใช้เช็คแบบปลอดภัย
    /// - ป้องกัน duplicate
    /// - ปลอดภัยตอนเปลี่ยนซีน/ออกเกม
    /// </summary>
    public abstract class NonAutoCreateSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static bool _isQuitting;
        private static bool _isSceneChanging;
        
        /// <summary>มี instance อยู่จริงและไม่ได้อยู่ระหว่างปิดเกม</summary>
        public static bool IsAlive => _instance != null && !_isQuitting;

        /// <summary>
        /// เข้าถึง instance แบบ “ไม่ auto-create”
        /// - ถ้ากำลังปิดเกม/เปลี่ยนซีน → คืน null
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_isQuitting || _isSceneChanging) return null;
                return _instance != null ? _instance : FindExisting();
            }
        }

        private static T FindExisting()
        {
            if (_isQuitting || _isSceneChanging) return null;
            _instance = FindObjectOfType<T>(includeInactive: true);
            return _instance;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            _isQuitting = false;
            _isSceneChanging = false;

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
                Debug.LogWarning($"[SceneSingleton<{typeof(T).Name}>] Duplicate detected, destroying {name}.");
                Destroy(gameObject);
                return;
            }
            _instance = this as T;
            // ไม่ DDOL — อยู่แค่ในซีนนี้
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
