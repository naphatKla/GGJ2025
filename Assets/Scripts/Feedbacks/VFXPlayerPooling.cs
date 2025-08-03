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

        [Header("Playback Mode")] [SerializeField]
        private bool stopOnDisable;

        [SerializeField] private FollowMode followMode = FollowMode.Once;
        [SerializeField] private RotationMode rotationMode = RotationMode.Identity;

        private CancellationTokenSource _cts;

        private void Awake()
        {
            var key = vfxPrefab.name;

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
            var vfxInstance = PoolingManager.Instance.Get<ParticleSystem>(vfxPrefab.name);
            vfxInstance.transform.position = transform.position;
            vfxInstance.transform.rotation = rotationMode switch
            {
                RotationMode.MatchOwner => transform.rotation,
                _ => Quaternion.identity
            };
            vfxInstance.gameObject.SetActive(true);
            
            PlayAndReleaseAsync(vfxInstance).Forget();
        }

        private async UniTaskVoid PlayAndReleaseAsync(ParticleSystem instance)
        {
            if (instance == null) return;

            instance.Play();

            if (followMode == FollowMode.Follow)
                await FollowWhileAlive(instance);
            else
                await UniTask.WaitWhile(() => instance.IsAlive(true), cancellationToken: GetLinkedToken());

            instance.Stop();
            instance.gameObject.SetActive(false);
            PoolingManager.Instance.Release(vfxPrefab.name, instance);
        }

        private async UniTask FollowWhileAlive(ParticleSystem instance)
        {
            var token = GetLinkedToken();

            try
            {
                while (instance.IsAlive(true))
                {
                    instance.transform.position = transform.position;

                    if (rotationMode == RotationMode.MatchOwner)
                        instance.transform.rotation = transform.rotation;

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