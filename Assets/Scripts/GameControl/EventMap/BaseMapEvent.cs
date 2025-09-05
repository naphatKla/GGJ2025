using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;

namespace GameControl.EventMap
{
    public abstract class BaseMapEvent : MonoBehaviour
    {
        public float deletetime;
        public float previewDuration;
        public float delayBeforePerformAfterPreview;
        public float damage;
        public ParticleSystem previewEffect;

        public bool debug;

        [SerializeField] protected MMF_Player notifyFeedback;
        [SerializeField] protected MMF_Player playFeedback;

        private IObjectPool<BaseMapEvent> _pool;
        private CancellationTokenSource _cts;

        public void SetPool(IObjectPool<BaseMapEvent> pool)
        {
            _pool = pool;
        }
        
        private void OnDestroy()
        {
            CancelPlay();
            _cts?.Dispose();
        }
        
        public void ApplyEffect(MapEventStorageEntry entry)
        {
            if (previewEffect == null) return;
            var main = previewEffect.main;
            float originalDuration = main.duration;
            main.simulationSpeed = originalDuration / entry.delayPerform;
            deletetime = entry.deleteTime;
            damage = entry.damage;

            
            switch (entry.hitboxType)
            { 
                case HitboxType.Box:
                    main.startSizeX = entry.boxSize.x;
                    main.startSizeY = entry.boxSize.y;
                    main.startSizeZ = 0;
                    break;
                case HitboxType.None:
                    break;
            }
        }

        public async UniTask Play(CancellationToken externalToken = default)
        {
            _cts?.Cancel();
            _cts?.Dispose();

            _cts = new CancellationTokenSource();
            
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, externalToken);
            var token = linkedCts.Token;

            try
            {
                if (debug) Debug.Log("Start Play");
                await PlayPreview();

                await UniTask.WaitForSeconds(delayBeforePerformAfterPreview,
                    cancellationToken: destroyCancellationToken);
                
                if (debug) Debug.Log("Perform & Feedback");
                
                playFeedback?.PlayFeedbacks();
                Perform();
               

                if (debug) Debug.Log("Deleting");
                ReleaseAfterPlay(token).Forget();
            }
            catch (OperationCanceledException)
            {
                if (debug) Debug.Log("Play Cancelled");
            }
        }

        private async UniTask ReleaseAfterPlay(CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(deletetime), cancellationToken: token);
                _pool?.Release(this);
                if (debug) Debug.Log("Deleted");
            }
            catch (OperationCanceledException)
            {
                if (debug) Debug.Log("Release Cancelled");
            }
        }

        public void CancelPlay()
        {
            _cts?.Cancel();
        }

        //Particle
        public abstract UniTask PlayPreview();
        //Projectile or Circle
        protected abstract void Perform();

        public void ApplyHitbox(MapEventStorageEntry entry)
        {
            switch (entry.hitboxType)
            {
                case HitboxType.Box:
                    if (this is IBoxHitbox box)
                    {
                        box.Size = entry.boxSize;
                        box.Offset = entry.boxOffset;
                    }
                    break;
                case HitboxType.Sphere:
                    if (this is ISphereHitbox sphere)
                    {
                        sphere.Radius = entry.sphereRadius;
                        sphere.Offset = entry.sphereOffset;
                    }
                    break;
                case HitboxType.Capsule:
                    if (this is ICapsuleHitbox capsule)
                    {
                        capsule.Radius = entry.capsuleRadius;
                        capsule.Height = entry.capsuleHeight;
                        capsule.Offset = entry.capsuleOffset;
                    }
                    break;
                case HitboxType.None:
                    break;
            }
        }
    }
}
