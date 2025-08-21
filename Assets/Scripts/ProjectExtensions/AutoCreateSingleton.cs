using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectExtensions
{
    /// <summary>
    /// Singleton แบบ "auto-create" สำหรับออบเจ็กต์ภายในซีน (ไม่ DontDestroyOnLoad)
    /// - Instance จะ auto-create เมื่อถูกเรียกครั้งแรก (เฉพาะเวลาปลอดภัย)
    /// - ระหว่างกำลังออกเกม/เปลี่ยนซีน จะไม่สร้าง (คืน null)
    /// - มี Current/IsAlive สำหรับเช็คโดยไม่ก่อให้เกิดการสร้าง
    /// - ป้องกัน duplicate
    /// </summary>
    public abstract class AutoCreateSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static bool _isQuitting;
        private static bool _isSceneChanging;
        private static bool _isCreating; // กัน re-entrancy ระหว่าง Awake เรียก Instance อีก

        /// <summary>คืน instance ถ้ามีอยู่จริง (ไม่สร้างเพิ่ม)</summary>
        public static T Current => _instance;

        /// <summary>มี instance อยู่จริงและไม่ได้อยู่ระหว่างปิดเกม</summary>
        public static bool IsAlive => _instance != null && !_isQuitting;

        /// <summary>
        /// เข้าถึง instance แบบ “auto-create”
        /// - ถ้ากำลังปิดเกม/เปลี่ยนซีน → คืน null (ไม่สร้าง)
        /// - ถ้าไม่มีในซีน → สร้าง GameObject ใหม่และแปะคอมโพเนนต์ T
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_isQuitting || _isSceneChanging) return null;
                if (_instance != null) return _instance;

                // ลองหาในซีนก่อน (รองรับกรณี dev วางไว้เอง)
                _instance = FindObjectOfType<T>(includeInactive: true);
                if (_instance != null) return _instance;

                // สร้างใหม่แบบปลอดภัย
                return CreateNewInstance();
            }
        }

        private static T CreateNewInstance()
        {
            if (_isQuitting || _isSceneChanging || _isCreating) return null;

            _isCreating = true;
            try
            {
                var go = new GameObject($"[{typeof(T).Name}] (AutoCreated)");
                var inst = go.AddComponent<T>();
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
                Debug.LogWarning($"[AutoCreateSceneSingleton<{typeof(T).Name}>] Duplicate detected, destroying {name}.");
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
