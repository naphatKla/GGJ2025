using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Dan.Main;
using UnityEngine;

namespace Leaderboard
{
    public class LeaderboardUploader : MonoBehaviour
    {
        public static LeaderboardUploader Instance { get; private set; }

        public static LeaderboardUploader Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject(nameof(LeaderboardUploader));
            Instance = go.AddComponent<LeaderboardUploader>();
            DontDestroyOnLoad(go);
            return Instance;
        }

        [Header("Retry / Backoff")] [Tooltip("จำนวนครั้งสูงสุดที่จะลองใหม่เมื่อเซิร์ฟเวอร์ล่ม/เน็ตล่ม")]
        public int maxRetries = 3;

        [Tooltip("ดีเลย์เริ่มต้นของ backoff (วินาที)")]
        public float initialBackoffSeconds = 2f;

        [Tooltip("ตัวคูณ backoff ในแต่ละครั้ง (เช่น 2.0 = 2s -> 4s -> 8s)")]
        public float backoffFactor = 2f;

        [Header("Name Rules")] [Tooltip("ความยาวชื่อสูงสุดที่จะส่งขึ้นลีดเดอร์บอร์ด")]
        public int maxNameLength = 16;

        [Tooltip("ชื่อเริ่มต้น ถ้าชื่อว่าง")] public string fallbackName = "Player";

        // ===== Queue Persist =====
        private const string QueueKey = "LEADERBOARD_PENDING_QUEUE_V1";

        [Serializable]
        private class Entry
        {
            public string name;
            public int score;
        }

        [Serializable]
        private class Wrapper
        {
            public List<Entry> list = new();
        }

        private readonly Queue<Entry> _pending = new();
        private bool _processingQueue;

        public delegate void UploadEntryFn(string name, int score, Action<bool> onSuccess, Action<string> onError);

        /// <summary>
        ///     ค่าเริ่มต้น: ใช้ Leaderboards.ThailandGameShow
        ///     ถ้าจะอัปบอร์ดอื่น ให้เปลี่ยนฟังก์ชันนี้ก่อนเรียก Upload
        /// </summary>
        public UploadEntryFn UploadFn = (name, score, ok, err) =>
            Leaderboards.ThailandGameShow.UploadNewEntry(name, score, ok, err);

        // ===== Unity =====
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadQueue();
            StartCoroutine(ProcessQueueRoutine());
        }

        /// <summary>
        ///     เรียกอัปโหลดคะแนน (อัตโนมัติ retry + เข้าคิวถ้าล้มเหลว)
        /// </summary>
        /// <param name="rawName">ชื่อ</param>
        /// <param name="score">คะแนน</param>
        /// <param name="onComplete">callback true=สำเร็จ, false=ยังไม่สำเร็จ (ถูกคิวไว้)</param>
        public void Upload(string rawName, int score, Action<bool> onComplete = null)
        {
            var name = SanitizeName(rawName);
            StartCoroutine(UploadRoutine(name, score, onComplete));
        }

        /// <summary> เรียกบังคับให้ลองอัปของที่ค้างอยู่อีกครั้ง </summary>
        public void TriggerProcessQueue()
        {
            if (!_processingQueue) StartCoroutine(ProcessQueueRoutine());
        }

        private IEnumerator UploadRoutine(string name, int score, Action<bool> onComplete)
        {
            var attempt = 0;
            var delay = Mathf.Max(0.1f, initialBackoffSeconds);

            while (true)
            {
                bool? ok = null;
                var retriable = false;
                string errMsg = null;

                UploadFn(name, score,
                    e => { ok = true; },
                    e =>
                    {
                        ok = false;
                        errMsg = e;
                    }
                );

                var timeout = 15f;
                while (ok == null && timeout > 0f)
                {
                    timeout -= Time.unscaledDeltaTime;
                    yield return null;
                }

                if (ok == null)
                {
                    ok = false;
                    errMsg = "Timeout or no callback";
                }

                if (ok.Value)
                {
                    Debug.Log($"[Leaderboard] Upload success: {name} => {score}");
                    onComplete?.Invoke(true);
                    yield break;
                }

                retriable = IsRetriableError(errMsg);

                attempt++;
                if (!retriable || attempt > maxRetries)
                {
                    Debug.LogWarning($"[Leaderboard] Upload failed ({name},{score}). queued. err={errMsg}");
                    Enqueue(name, score);
                    onComplete?.Invoke(false);
                    yield break;
                }

                yield return new WaitForSecondsRealtime(delay);
                delay *= Mathf.Max(1.01f, backoffFactor);
            }
        }

        private IEnumerator ProcessQueueRoutine()
        {
            _processingQueue = true;
            var safeCap = 100;
            while (_pending.Count > 0 && safeCap-- > 0)
            {
                var e = _pending.Peek();
                var done = false;
                var success = false;
                string errMsg = null;

                UploadFn(e.name, e.score,
                    e =>
                    {
                        success = true;
                        done = true;
                    },
                    err =>
                    {
                        errMsg = err;
                        done = true;
                    }
                );

                var timeout = 15f;
                while (!done && timeout > 0f)
                {
                    timeout -= Time.unscaledDeltaTime;
                    yield return null;
                }

                if (!done) errMsg = "Timeout or no callback";

                if (success)
                {
                    _pending.Dequeue();
                    SaveQueue();
                }
                else
                {
                    if (IsRetriableError(errMsg))
                    {
                        yield return new WaitForSecondsRealtime(5f);
                    }
                    else
                    {
                        Debug.LogError($"[Leaderboard] Permanent failure on queued item: {errMsg}. Dropped.");
                        _pending.Dequeue();
                        SaveQueue();
                    }
                }
            }

            _processingQueue = false;
        }

        // ===== Helpers =====

        private static bool IsRetriableError(string message)
        {
            if (string.IsNullOrEmpty(message)) return true;
            var m = message.ToLowerInvariant();
            return m.Contains("service unavailable") ||
                   m.Contains("application error") ||
                   m.Contains("timeout") ||
                   m.Contains("timed out") ||
                   m.Contains("502") ||
                   m.Contains("503") ||
                   m.Contains("504") ||
                   m.Contains("429") ||
                   m.Contains("failed to connect") ||
                   m.Contains("could not resolve");
        }

        private string SanitizeName(string raw)
        {
            var name = string.IsNullOrWhiteSpace(raw) ? fallbackName : raw.Trim();
            name = Regex.Replace(name, @"\p{C}+", "");
            if (name.Length > maxNameLength) name = name.Substring(0, maxNameLength);
            if (string.IsNullOrWhiteSpace(name)) name = fallbackName;
            return name;
        }

        private void Enqueue(string name, int score)
        {
            _pending.Enqueue(new Entry { name = name, score = score });
            SaveQueue();
            if (!_processingQueue) StartCoroutine(ProcessQueueRoutine());
        }

        private void SaveQueue()
        {
            var w = new Wrapper { list = new List<Entry>(_pending) };
            var json = JsonUtility.ToJson(w);
            PlayerPrefs.SetString(QueueKey, json);
            PlayerPrefs.Save();
        }

        private void LoadQueue()
        {
            var json = PlayerPrefs.GetString(QueueKey, "");
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var w = JsonUtility.FromJson<Wrapper>(json);
                if (w?.list != null)
                    foreach (var e in w.list)
                        _pending.Enqueue(e);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Leaderboard] LoadQueue failed: {e.Message}");
                PlayerPrefs.DeleteKey(QueueKey);
            }
        }
    }
}