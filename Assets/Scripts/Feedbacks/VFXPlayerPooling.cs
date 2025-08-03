using Manager;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

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

    public class VFXPlayerPooling : MonoBehaviour, IPoolingLifeCycle<ParticleSystem>
    {
        [Header("VFX Setup")]
        [SerializeField] private ParticleSystem vfxPrefab;

        [Header("Playback Mode")]
        [SerializeField] private FollowMode followMode = FollowMode.Once;
        [SerializeField] private RotationMode rotationMode = RotationMode.Identity;

        private static readonly HashSet<string> CreatedPools = new();

        private void Awake()
        {
            var key = vfxPrefab.name;
            
            if (CreatedPools.Contains(key)) return;
            
            PoolingManager.Instance.Create<ParticleSystem>(
                key,
                PoolingGroupName.VFX,
                this
            );
            
            CreatedPools.Add(key);
        }

        public void PlayVFX()
        {
            var vfx = PoolingManager.Instance.Get<ParticleSystem>(vfxPrefab.name);
            PlayAndReleaseAsync(vfx).Forget();
        }

        private async UniTaskVoid PlayAndReleaseAsync(ParticleSystem instance)
        {
            if (instance == null) return;

            instance.Play();

            if (followMode == FollowMode.Follow)
                await FollowWhileAlive(instance);
            else
                await UniTask.WaitWhile(() => instance.IsAlive(true), cancellationToken: this.GetCancellationTokenOnDestroy());

            PoolingManager.Instance.Release(vfxPrefab.name, instance);
        }

        private async UniTask FollowWhileAlive(ParticleSystem instance)
        {
            while (instance.IsAlive(true))
            {
                instance.transform.position = transform.position;

                if (rotationMode == RotationMode.MatchOwner)
                    instance.transform.rotation = transform.rotation;

                await UniTask.NextFrame(cancellationToken: this.GetCancellationTokenOnDestroy());
            }
        }

        // Pool life cycle
        public ParticleSystem CreatePoolInstance()
        {
            var vfxInstance = Instantiate(vfxPrefab);
            vfxInstance.transform.position = transform.position;
            vfxInstance.gameObject.SetActive(false);
            return vfxInstance;
        }

        public void OnGetFromPool(ParticleSystem instance)
        {
            instance.transform.position = transform.position;
            instance.transform.rotation = rotationMode switch
            {
                RotationMode.MatchOwner => transform.rotation,
                _ => Quaternion.identity
            };
            instance.gameObject.SetActive(true);
        }

        public void OnReleaseToPool(ParticleSystem instance)
        {
            instance.gameObject.SetActive(false);
            instance.Stop();
        }

        public void OnDestroyFromPool(ParticleSystem instance)
        {
            Destroy(instance.gameObject);
        }
    }
}
