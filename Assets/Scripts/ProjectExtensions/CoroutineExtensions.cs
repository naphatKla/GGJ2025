using System;
using System.Collections;

namespace ProjectExtensions
{
    /// <summary>
    /// Extension methods for IEnumerator to allow callback execution after coroutine completion.
    /// </summary>
    public static class CoroutineExtensions
    {
        /// <summary>
        /// Attaches a callback to be executed after the given coroutine finishes.
        /// </summary>
        /// <param name="routine">The coroutine to run.</param>
        /// <param name="onComplete">The callback to invoke after the coroutine ends.</param>
        /// <returns>A new IEnumerator that executes the callback after the routine finishes.</returns>
        public static IEnumerator WithCallback(this IEnumerator routine, Action onComplete)
        {
            if (routine == null)
            {
                onComplete?.Invoke(); // ป้องกัน null routine
                yield break;
            }

            yield return routine;
            onComplete?.Invoke();
        }
    }
}