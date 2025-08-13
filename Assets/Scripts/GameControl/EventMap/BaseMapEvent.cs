using System;
using Cysharp.Threading.Tasks;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.Pool;

namespace GameControl.EventMap
{
    public abstract class BaseMapEvent : MonoBehaviour
    {
        public float deletetime;
        public float delayBeforePerform;
        public float damage;

        public bool debug;

        [SerializeField] protected MMF_Player feedback;

        private IObjectPool<BaseMapEvent> _pool;

        public void SetPool(IObjectPool<BaseMapEvent> pool)
        {
            _pool = pool;
        }

        public async UniTask Play()
        {
            //effect before perform
            if (debug) Debug.Log("Start Play");
            await PlayPreview();

            if (debug) Debug.Log("Perform & Feedback");
            Perform();
            feedback?.PlayFeedbacks();

            if (debug) Debug.Log("Deleting");
            ReleaseAfterPlay().Forget();
            if (debug) Debug.Log("Deleted");
        }

        private async UniTask ReleaseAfterPlay()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(deletetime));
            _pool?.Release(this);
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