using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Manager;
using UnityEngine;

namespace Feedbacks
{
    public enum FollowMode
    {
        Once = 0,
        Follow = 1,
    }

    public enum RotationMode
    {
        Identity = 0,
        MatchOwner = 1,
    }

    public class VFXPlayerPooling : MonoBehaviour
    {
        [Header("VFX Setup")] [SerializeField] private ParticleSystem vfxPrefab;
        [SerializeField] private Transform overrideOwner;

        [Header("Playback Mode")] [SerializeField]
        private bool stopOnDisable;

        [SerializeField] private FollowMode followMode = FollowMode.Once;
        [SerializeField] private RotationMode rotationMode = RotationMode.Identity;

        private CancellationTokenSource _cts;
        private ParticleSystem _currentVFXInstance;
        public ParticleSystem CurrentVFXInstance => _currentVFXInstance;

        private void Awake()
        {
            var key = vfxPrefab.name;
            
            if (!overrideOwner)
                overrideOwner = transform;

            PoolingManager.Instance.Create<ParticleSystem>(
                key,
                PoolingGroupName.VFX,
                CreatePoolInstance
            );
        }

        private void OnDisable()
        {
            if (stopOnDisable)
                _cts?.Cancel();
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        public void PlayVFX()
        {
            _currentVFXInstance = PoolingManager.Instance.Get<ParticleSystem>(vfxPrefab.name);
            _currentVFXInstance.transform.position = overrideOwner.position;
            _currentVFXInstance.transform.rotation = rotationMode switch
            {
                RotationMode.MatchOwner => overrideOwner.rotation,
                _ => Quaternion.identity
            };
            _currentVFXInstance.gameObject.SetActive(true);
            
            PlayAndReleaseAsync(_currentVFXInstance).Forget();
        }

        private async UniTaskVoid PlayAndReleaseAsync(ParticleSystem instance)
        {
            if (instance == null) return;
            var token = GetLinkedToken();

            try
            {
                instance.Play();

                if (followMode == FollowMode.Follow)
                    await FollowWhileAlive(instance);
                else
                    await UniTask.WaitWhile(() => instance.IsAlive(true) && instance, cancellationToken: token);

                // If instance was destroyed during waiting
                if (!instance) return;
                
                instance.Stop();
                instance.gameObject.SetActive(false);
                PoolingManager.Instance.Release(vfxPrefab.name, instance);
                _currentVFXInstance = null;
            }
            catch (OperationCanceledException)
            {
                // Canceled via disable/destroy
            }
        }

        private async UniTask FollowWhileAlive(ParticleSystem instance)
        {
            var token = GetLinkedToken();

            try
            {
                while (instance.IsAlive(true))
                {
                    instance.transform.position = overrideOwner.position;

                    if (rotationMode == RotationMode.MatchOwner)
                        instance.transform.rotation = overrideOwner.rotation;

                    await UniTask.NextFrame(cancellationToken: token);
                }
            }
            catch (OperationCanceledException)
            {
                // Canceled via disable/destroy
            }
        }

        private CancellationToken GetLinkedToken()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            return CancellationTokenSource.CreateLinkedTokenSource(
                _cts.Token,
                this.GetCancellationTokenOnDestroy()
            ).Token;
        }

        public ParticleSystem CreatePoolInstance()
        {
            var vfxInstance = Instantiate(vfxPrefab);
            vfxInstance.gameObject.SetActive(false);
            return vfxInstance;
        }
    }
}