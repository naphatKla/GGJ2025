using UnityEngine;

namespace ProjectExtensions
{
    /// <summary>
    /// Singleton ภายในซีน (ไม่ DDOL) แบบ "ไม่ auto-create"
    /// - Instance: คืนตัวที่มีอยู่/Find เท่านั้น (ไม่สร้างใหม่)
    /// - Current: อ้างอิงโดยไม่กระตุ้นการค้นหา/สร้าง
    /// - IsAlive: แค่เช็คว่ามีอินสแตนซ์อยู่ไหม
    /// - Debug: เตือนถ้าเรียกจาก teardown, เตือน duplicate
    /// </summary>
    public abstract class NonAutoCreateSingleton<T> : MonoBehaviour where T : MonoBehaviour
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
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}