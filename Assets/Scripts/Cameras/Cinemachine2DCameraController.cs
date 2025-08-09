using System;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
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
        [LabelText("Lerp Duration")]
        [MinValue(0f)]
        public float duration = 0.5f;
    }

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

        private CinemachineVirtualCamera currentCam;
        private float defaultOrthoSize;
        private float defaultFOV;
        private float defaultFollowDamping;
        private Transform defaultFollowTarget;
        private Transform defaultLookAtTarget;

        private CancellationTokenSource orthoSizeCTS;
        private bool _isInit;
        private float _nextShakeTime = 0f;

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
        }

        private void CacheDefaults()
        {
            defaultOrthoSize = currentCam.m_Lens.OrthographicSize;
            defaultFOV = currentCam.m_Lens.FieldOfView;
            defaultFollowTarget = currentCam.Follow;
            defaultLookAtTarget = currentCam.LookAt;

            var transposer = currentCam.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (transposer != null)
                defaultFollowDamping = transposer.m_XDamping;
        }

        public async UniTask LerpOrthoSize(float targetSize, float duration)
        {
            if (currentCam == null) return;

            orthoSizeCTS?.Cancel();
            orthoSizeCTS = new CancellationTokenSource();

            float startSize = currentCam.m_Lens.OrthographicSize;
            float time = 0;

            try
            {
                while (time < duration)
                {
                    time += Time.deltaTime;
                    float t = time / duration;
                    currentCam.m_Lens.OrthographicSize = Mathf.Lerp(startSize, targetSize, t);
                    await UniTask.Yield(PlayerLoopTiming.Update, orthoSizeCTS.Token);
                }

                currentCam.m_Lens.OrthographicSize = targetSize;
            }
            catch (OperationCanceledException) { }
        }

        public UniTask LerpOrthoSize(CameraOrthoOption option)
        {
            return LerpOrthoSize(option.targetSize, option.duration);
        }

        public void ResetCamera(float duration = 0.5f)
        {
            if (!_isInit || currentCam == null) return;

            orthoSizeCTS?.Cancel();
            LerpOrthoSize(defaultOrthoSize, duration).Forget();

            currentCam.Follow = defaultFollowTarget;
            currentCam.LookAt = defaultLookAtTarget;

            var transposer = currentCam.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (transposer != null)
                LerpDamping(transposer, transposer.m_XDamping, defaultFollowDamping, duration).Forget();
        }

        private async UniTaskVoid LerpDamping(CinemachineFramingTransposer transposer, float start, float target, float duration)
        {
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = time / duration;
                transposer.m_XDamping = Mathf.Lerp(start, target, t);
                await UniTask.Yield();
            }

            transposer.m_XDamping = target;
        }

        public void SetFollowSpeed(float newDamping)
        {
            if (currentCam == null) return;

            var transposer = currentCam.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (transposer != null)
                transposer.m_XDamping = newDamping;
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
            if (option == null)
            {
                ShakeCamera();
                return;
            }

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
