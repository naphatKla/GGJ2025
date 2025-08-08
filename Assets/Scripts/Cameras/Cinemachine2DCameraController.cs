using System;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Cameras
{
    public class Cinemachine2DCameraController : MonoBehaviour
    {
        [Header("Camera References")] [SerializeField]
        private CinemachineVirtualCamera[] virtualCameras;

        private bool _isInit;
        private CinemachineVirtualCamera currentCam;
        private float defaultOrthoSize;
        private float defaultFOV;
        private float defaultFollowDamping;
        private Transform defaultFollowTarget;
        private Transform defaultLookAtTarget;

        private CancellationTokenSource orthoSizeCTS;
        private CancellationTokenSource fovCTS;

        private void Awake()
        {
            if (virtualCameras == null || virtualCameras.Length == 0)
            {
                Debug.LogError("[CameraController] No virtual cameras assigned!");
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
            {
                defaultFollowDamping = transposer.m_XDamping;
            }
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
            catch (OperationCanceledException)
            {
                // Lerp interrupted
            }
        }

        public async UniTask LerpFOV(float targetFOV, float duration)
        {
            if (currentCam == null) return;

            fovCTS?.Cancel();
            fovCTS = new CancellationTokenSource();

            float startFOV = currentCam.m_Lens.FieldOfView;
            float time = 0;

            try
            {
                while (time < duration)
                {
                    time += Time.deltaTime;
                    float t = time / duration;
                    currentCam.m_Lens.FieldOfView = Mathf.Lerp(startFOV, targetFOV, t);
                    await UniTask.Yield(PlayerLoopTiming.Update, fovCTS.Token);
                }

                currentCam.m_Lens.FieldOfView = targetFOV;
            }
            catch (OperationCanceledException)
            {
                // Lerp interrupted
            }
        }

        public void SetFollowSpeed(float newDamping)
        {
            if (currentCam == null) return;

            var transposer = currentCam.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (transposer != null)
            {
                transposer.m_XDamping = newDamping;
            }
        }

        public void SetFollowTarget(Transform target)
        {
            if (currentCam == null) return;
            currentCam.Follow = target;
        }

        public void ResetCamera(float duration = 0.5f)
        {
            if (!_isInit || currentCam == null) return;

            orthoSizeCTS?.Cancel();
            fovCTS?.Cancel();

            // Start new lerps
            LerpOrthoSize(defaultOrthoSize, duration).Forget();
            LerpFOV(defaultFOV, duration).Forget();

            // Restore follow/lookAt
            currentCam.Follow = defaultFollowTarget;
            currentCam.LookAt = defaultLookAtTarget;

            // Damping reset
            var transposer = currentCam.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (transposer != null)
            {
                float startDamping = transposer.m_XDamping;
                float targetDamping = defaultFollowDamping;
                LerpDamping(transposer, startDamping, targetDamping, duration).Forget();
            }
        }

        private async UniTaskVoid LerpDamping(CinemachineFramingTransposer transposer, float start, float target,
            float duration)
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

        public Vector3 GetCameraCenterWorldPosition()
        {
            if (currentCam == null || currentCam.VirtualCameraGameObject == null)
                return Vector3.zero;

            return currentCam.VirtualCameraGameObject.transform.position;
        }
    }
}