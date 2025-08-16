using System;
using System.Collections.Generic;
using Cinemachine;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Cameras
{
    [Serializable]
    public class CameraShakeOption
    {
        [BoxGroup("Shake Settings")]
        [LabelText("Shake Force")]
        [MinValue(0f)]
        public float force = 15f;

        [BoxGroup("Shake Settings")]
        [LabelText("Shake Frequency")]
        [MinValue(0f)]
        public float frequency = 0.1f;

        [BoxGroup("Shake Settings")]
        [LabelText("Shake Duration")]
        [MinValue(0f)]
        public float duration = 0.3f;
    }

    [Serializable]
    public class CameraOrthoOption
    {
        [BoxGroup("Ortho Settings")]
        [LabelText("Target Size")]
        [MinValue(0f)]
        public float targetSize = 13.75f;

        [BoxGroup("Ortho Settings")]
        [LabelText("Blend Duration")]
        [MinValue(0f)]
        public float duration = 0.5f;
    }

    /// <summary>
    /// Cinemachine controller ที่รวม:
    /// - Ortho scheduler (หลายแหล่งขอพร้อมกัน → เลือก "ขนาดใหญ่สุด" เสมอ)
    /// - Blend ภายในตัวเดียว (ไม่ใช้คอร์รูทีน/ยกเลิกกัน)
    /// - ยกเลิกคำขอแบบราย handle หรือทั้ง owner ได้
    /// - Shake camera ด้วย CinemachineImpulse
    /// - Lerp follow damping ภายใน (ไม่ใช้ UniTask)
    /// </summary>
    public class Cinemachine2DCameraController : MonoBehaviour
    {
        [Header("Camera References")]
        [SerializeField] private CinemachineVirtualCamera[] virtualCameras;

        [Header("Impulse Source")]
        [SerializeField] private CinemachineImpulseSource impulseSource;

        [Header("Shake Config")]
        [SerializeField] private float defaultShakeForce = 15f;
        [SerializeField] private float defaultShakeDuration = 0.3f;
        [SerializeField] private float defaultShakeFrequency = 1f;
        [SerializeField] private float defaultShakeCooldown = 0.1f;

        [Header("Ortho Blending")]
        [Tooltip("ระยะเวลาพื้นฐานในการ blend ไปยังค่าเป้าหมาย (วินาที)")]
        [SerializeField] private float defaultBlendTime = 0.25f;

        private CinemachineVirtualCamera currentCam;

        // defaults
        public float defaultOrthoSize;
        private float defaultFOV;
        private float defaultFollowDamping;
        private Transform defaultFollowTarget;
        private Transform defaultLookAtTarget;
        private CinemachineFramingTransposer _transposer;

        private bool _isInit;
        private float _nextShakeTime = 0f;

        public CinemachineVirtualCamera CurrentCam => currentCam;

        // ---------- Ortho scheduler ----------
        private struct OrthoReq
        {
            public int id;
            public float size;
            public float expireAt;   // Time.time เมื่อหมดอายุ
            public object owner;     // ตัวต้นทาง (สกิล/ระบบ) ที่ขอ
        }

        private readonly List<OrthoReq> _requests = new();
        private int _nextReqId = 1;

        // blending state
        private float _blendFrom;
        private float _blendTo;
        private float _blendT;     // 0..1
        private float _blendDur;
        private float _lastOrthoApplied;

        // ---------- Damping blend ----------
        private float _dampFrom, _dampTo, _dampT, _dampDur;

        private void Awake()
        {
            if (virtualCameras == null || virtualCameras.Length == 0)
            {
                Debug.LogError("[CameraController] No virtual cameras assigned!");
                enabled = false;
                return;
            }

            if (impulseSource == null)
            {
                Debug.LogError("[CameraController] No impulse source assigned!");
                enabled = false;
                return;
            }

            SetActiveCamera(0);
            _isInit = true;
        }

        public void SetActiveCamera(int index)
        {
            if (index < 0 || index >= virtualCameras.Length)
            {
                Debug.LogError($"[CameraController] Invalid camera index: {index}");
                return;
            }

            for (int i = 0; i < virtualCameras.Length; i++)
                virtualCameras[i].gameObject.SetActive(i == index);

            currentCam = virtualCameras[index];
            CacheDefaults();

            // ตั้งค่า blending ให้ต่อเนื่อง ไม่เด้ง
            float cur = currentCam.m_Lens.OrthographicSize;
            _lastOrthoApplied = cur;
            _blendFrom = cur;
            _blendTo = cur;
            _blendT = 1f;
            _blendDur = defaultBlendTime;

            // damping
            _dampFrom = _transposer != null ? _transposer.m_XDamping : 0f;
            _dampTo = _dampFrom;
            _dampT = 1f;
            _dampDur = 0.1f;
        }

        private void CacheDefaults()
        {
            defaultOrthoSize = currentCam.m_Lens.OrthographicSize;
            defaultFOV = currentCam.m_Lens.FieldOfView;
            defaultFollowTarget = currentCam.Follow;
            defaultLookAtTarget = currentCam.LookAt;

            _transposer = currentCam.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (_transposer != null)
                defaultFollowDamping = _transposer.m_XDamping;
        }

        private void Update()
        {
            TickOrthoScheduler();
            TickDampingBlend();
        }

        // =========================================================
        // ORTHO SCHEDULER (MAX PRIORITY)
        // =========================================================

        /// <summary>
        /// ขอ Ortho ชั่วคราว (ขนาด, ระยะเวลา). คืน handle สำหรับยกเลิกภายหลัง
        /// </summary>
        public int PushOrtho(float size, float duration, object owner = null, float? blendOverride = null)
        {
            if (currentCam == null) return -1;
            if (size <= 0f) size = defaultOrthoSize;
            if (duration <= 0f) duration = 0.0001f;

            var req = new OrthoReq
            {
                id = _nextReqId++,
                size = size,
                expireAt = Time.time + duration,
                owner = owner
            };
            _requests.Add(req);

            // ถ้าขนาดใหม่ใหญ่กว่าเป้าหมาย → เริ่ม blend ทันที
            float desired = ComputeDesiredOrtho();
            BeginOrthoBlendTo(desired, blendOverride ?? defaultBlendTime);
            return req.id;
        }

        /// <summary>
        /// ขอ Ortho จาก option (สะดวกเวลาเรียกจาก data)
        /// </summary>
        public int PushOrtho(CameraOrthoOption option, object owner = null)
        {
            if (option == null) return -1;
            return PushOrtho(option.targetSize, option.duration, owner);
        }

        /// <summary>
        /// ยกเลิก request โดย handle
        /// </summary>
        public void CancelRequest(int handle)
        {
            if (handle <= 0) return;
            for (int i = 0; i < _requests.Count; i++)
            {
                if (_requests[i].id == handle)
                {
                    _requests.RemoveAt(i);
                    float desired = ComputeDesiredOrtho();
                    BeginOrthoBlendTo(desired, defaultBlendTime);
                    return;
                }
            }
        }

        /// <summary>
        /// ยกเลิกทุก request ของ owner
        /// </summary>
        public void CancelByOwner(object owner)
        {
            _requests.RemoveAll(r => ReferenceEquals(r.owner, owner));
            float desired = ComputeDesiredOrtho();
            BeginOrthoBlendTo(desired, defaultBlendTime);
        }

        /// <summary>
        /// ล้างทุก request และกลับสู่ default
        /// </summary>
        public void ResetAndClearAllRequests(float? blend = null)
        {
            _requests.Clear();
            BeginOrthoBlendTo(defaultOrthoSize, blend ?? defaultBlendTime);
        }

        /// <summary>
        /// (Deprecated) เวอร์ชันเก่า—ตอนนี้จะ push request แทนการยกเลิกงานคนอื่น
        /// </summary>
        [Obsolete("Use PushOrtho(targetSize, duration, owner) instead. This method now just pushes a request.")]
        public void LerpOrthoSize(float targetSize, float duration)
        {
            PushOrtho(targetSize, duration, owner: null);
        }

        /// <summary>
        /// (Deprecated) เวอร์ชันเก่า—ตอนนี้จะ push request แทน
        /// </summary>
        [Obsolete("Use PushOrtho(option.targetSize, option.duration, owner) instead.")]
        public void LerpOrthoSize(CameraOrthoOption option)
        {
            if (option == null) return;
            PushOrtho(option.targetSize, option.duration, owner: null);
        }
        

        private void TickOrthoScheduler()
        {
            if (currentCam == null) return;

            // 1) ไถคำขอที่หมดเวลาออก
            float now = Time.time;
            bool removed = false;
            for (int i = _requests.Count - 1; i >= 0; i--)
            {
                if (now >= _requests[i].expireAt)
                {
                    _requests.RemoveAt(i);
                    removed = true;
                }
            }

            // 2) ถ้ามีการเปลี่ยนแปลง → คำนวณเป้าหมายใหม่
            if (removed)
            {
                float desired = ComputeDesiredOrtho();
                BeginOrthoBlendTo(desired, defaultBlendTime);
            }

            // 3) ดำเนินการ blend
            if (_blendT < 1f)
            {
                _blendT = Mathf.Min(1f, _blendT + (Time.deltaTime / Mathf.Max(0.0001f, _blendDur)));
                float val = Mathf.Lerp(_blendFrom, _blendTo, _blendT);
                if (!Mathf.Approximately(val, _lastOrthoApplied))
                {
                    currentCam.m_Lens.OrthographicSize = val;
                    _lastOrthoApplied = val;
                }
            }
            else
            {
                // กันกรณีค่าภายนอกเปลี่ยน
                float desired = ComputeDesiredOrtho();
                if (!Mathf.Approximately(desired, _lastOrthoApplied))
                    BeginOrthoBlendTo(desired, defaultBlendTime);
            }
        }

        private float ComputeDesiredOrtho()
        {
            float top = defaultOrthoSize; // default เป็นฐาน
            for (int i = 0; i < _requests.Count; i++)
                if (_requests[i].size > top)
                    top = _requests[i].size;
            return top;
        }

        private void BeginOrthoBlendTo(float target, float duration)
        {
            float cur = currentCam.m_Lens.OrthographicSize;
            _blendFrom = cur;
            _blendTo = target;
            _blendDur = Mathf.Max(0.0001f, duration);
            _blendT = 0f;
        }

        // =========================================================
        // DAMPING BLEND (ไม่ใช้คอร์รูทีน)
        // =========================================================

        private void BeginDampingBlend(float from, float to, float duration)
        {
            if (_transposer == null) return;
            _dampFrom = from;
            _dampTo = to;
            _dampDur = Mathf.Max(0.0001f, duration);
            _dampT = 0f;
        }

        private void TickDampingBlend()
        {
            if (_transposer == null) return;
            if (_dampT >= 1f) return;

            _dampT = Mathf.Min(1f, _dampT + (Time.deltaTime / Mathf.Max(0.0001f, _dampDur)));
            _transposer.m_XDamping = Mathf.Lerp(_dampFrom, _dampTo, _dampT);
        }

        // =========================================================
        // OTHER UTILS
        // =========================================================

        public void SetFollowSpeed(float newDamping)
        {
            if (_transposer != null)
                _transposer.m_XDamping = newDamping;
        }

        public void SetFollowTarget(Transform target)
        {
            if (currentCam == null) return;
            currentCam.Follow = target;
        }

        public Vector3 GetCameraCenterWorldPosition()
        {
            if (currentCam == null || currentCam.VirtualCameraGameObject == null)
                return Vector3.zero;
            return currentCam.VirtualCameraGameObject.transform.position;
        }

        // =========================================================
        // SHAKE
        // =========================================================

        public void ShakeCamera(float force = -1f, float? frequency = null, float? duration = null)
        {
            if (Time.realtimeSinceStartup < _nextShakeTime) return;
            if (impulseSource == null)
            {
                Debug.LogWarning("[CameraController] No impulse source found for shake!");
                return;
            }

            float actualForce = force < 0 ? defaultShakeForce : force;
            float actualFreq = frequency ?? defaultShakeFrequency;
            float actualDur = duration ?? defaultShakeDuration;

            var def = impulseSource.m_ImpulseDefinition;
            def.m_FrequencyGain = actualFreq;
            def.m_ImpulseDuration = actualDur;

            impulseSource.m_DefaultVelocity = Vector3.one * actualForce;
            impulseSource.GenerateImpulse();

            _nextShakeTime = Time.realtimeSinceStartup + defaultShakeCooldown;
        }

        public void ShakeCamera(CameraShakeOption option)
        {
            if (Time.realtimeSinceStartup < _nextShakeTime) return;
            if (option == null) { ShakeCamera(); return; }

            float force = option.force <= 0 ? defaultShakeForce : option.force;
            float frequency = Mathf.Max(option.frequency, 0f);
            float duration = Mathf.Max(option.duration, 0.01f);

            var def = impulseSource.m_ImpulseDefinition;
            def.m_FrequencyGain = frequency;
            def.m_ImpulseDuration = duration;

            impulseSource.m_DefaultVelocity = Vector3.one * force;
            impulseSource.GenerateImpulse();

            _nextShakeTime = Time.realtimeSinceStartup + defaultShakeCooldown;
        }
    }
}
