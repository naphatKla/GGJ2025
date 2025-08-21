//#define SINGLETON_DEBUG  // จะบังคับเปิดตรงนี้ หรือไปตั้งใน Player Settings > Scripting Define Symbols ก็ได้

using System.Diagnostics;
using System.Linq;

namespace ProjectExtensions
{
    internal static class SingletonDiagnostics
    {
#if SINGLETON_DEBUG
        public static bool Enabled = true;
#else
        public static bool Enabled = false;
#endif

        public static bool IsInTearDownStack()
        {
#if SINGLETON_DEBUG
            var st = new StackTrace();
            var tearNames = new[] { "OnDestroy", "OnDisable", "OnApplicationQuit" };
            return st.GetFrames()?.Any(f => tearNames.Contains(f.GetMethod().Name)) ?? false;
#else
            return false;
#endif
        }

        public static void LogAutoCreate<T>(string reason) where T : UnityEngine.MonoBehaviour
        {
#if SINGLETON_DEBUG
            if (!Enabled) return;
            UnityEngine.Debug.LogWarning($"[Singleton<{typeof(T).Name}>] Auto-Create → {reason}\n{ShortStackTrace(12)}");
#endif
        }

        public static void LogTeardownAccess<T>() where T : UnityEngine.MonoBehaviour
        {
#if SINGLETON_DEBUG
            if (!Enabled) return;
            UnityEngine.Debug.LogWarning($"[Singleton<{typeof(T).Name}>] Instance accessed from teardown (OnDestroy/OnDisable?)\n{ShortStackTrace(10)}");
#endif
        }

        public static void LogDuplicate<T>(UnityEngine.Object duplicate) where T : UnityEngine.MonoBehaviour
        {
#if SINGLETON_DEBUG
            if (!Enabled) return;
            UnityEngine.Debug.LogWarning($"[Singleton<{typeof(T).Name}>] Duplicate detected, destroying: {duplicate.name}\n{ShortStackTrace(8)}");
#endif
        }

        private static string ShortStackTrace(int frames)
        {
            var st = new StackTrace(true);
            var lines = st.GetFrames()?
                .Take(frames)
                .Select(f => $"  at {f.GetMethod().DeclaringType?.FullName}.{f.GetMethod().Name} ({f.GetFileName()}:{f.GetFileLineNumber()})");
            return lines == null ? "" : string.Join("\n", lines);
        }
    }
}
