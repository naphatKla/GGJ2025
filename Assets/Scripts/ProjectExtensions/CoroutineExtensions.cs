using System;
using System.Collections;

namespace ProjectExtensions
{
    public static class CoroutineExtensions
    {
        public static IEnumerator WithCallback(this IEnumerator routine, Action onComplete)
        {
            yield return routine;
            onComplete?.Invoke();
        }
    }
}
