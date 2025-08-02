using Manager;
using UnityEngine;
using Cysharp.Threading.Tasks;

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
        [Header("VFX Setup")]
        [SerializeField] private ParticleSystem vfxPrefab;

        [Header("Playback Mode")]
        [SerializeField] private FollowMode followMode = FollowMode.Once;
        [SerializeField] private RotationMode rotationMode = RotationMode.Identity;

        private void Awake()
        {
            PoolingManager.Instance.Create<ParticleSystem>(
                vfxPrefab.name,
                vfxPrefab.gameObject,
                PoolingGroupName.VFX
            );
        }

        public void PlayVFX()
        {
            var vfx = PoolingManager.Instance.Get<ParticleSystem>(vfxPrefab.name);

            vfx.transform.position = transform.position;
            vfx.transform.rotation = rotationMode switch
            {
                RotationMode.MatchOwner => transform.rotation,
                _ => Quaternion.identity
            };

            PlayAndReleaseAsync(vfx, vfxPrefab).Forget();
        }

        private async UniTaskVoid PlayAndReleaseAsync(ParticleSystem instance, ParticleSystem prefab)
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
    }
}
