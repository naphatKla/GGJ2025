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

        private CinemachineVirtualCamera currentCam;
        private float defaultOrthoSize;
        private float defaultFOV;
        private float defaultFollowDamping;

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

        public void ResetLerpValues()
        {
            if (currentCam == null) return;

            orthoSizeCTS?.Cancel();
            fovCTS?.Cancel();

            currentCam.m_Lens.OrthographicSize = defaultOrthoSize;
            currentCam.m_Lens.FieldOfView = defaultFOV;

            var transposer = currentCam.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (transposer != null)
            {
                transposer.m_XDamping = defaultFollowDamping;
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

        public void ResetCamera()
        {
            if (currentCam == null) return;

            orthoSizeCTS?.Cancel();
            fovCTS?.Cancel();

            currentCam.m_Lens.OrthographicSize = defaultOrthoSize;
            currentCam.m_Lens.FieldOfView = defaultFOV;

            currentCam.Follow = null;
            currentCam.LookAt = null;

            var transposer = currentCam.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (transposer != null)
            {
                transposer.m_XDamping = defaultFollowDamping;
            }
        }
    }
}