using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;

namespace GameControl.EventMap
{
    public abstract class BaseMapEvent : MonoBehaviour
    {
        [ReadOnly][ShowInInspector] private float _overAllTime;
        [SerializeField] protected string mapEventId;
        public int prewarmCount;
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
        public string MapEventId => mapEventId;
        protected MapEventStorageEntry currentEntry;
        protected CancellationToken _playToken;

        private void OnValidate()
        {
            _overAllTime = deletetime + previewDuration + delayBeforePerformAfterPreview;
        }

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
            currentEntry      = entry;
            deletetime       = entry.deleteTime;
            damage           = entry.damage;
            previewDuration  = entry.delayPerform;
            
            if (previewEffect != null)
            {
                var main = previewEffect.main;
                float originalDuration = main.duration;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.simulationSpeed = originalDuration / Mathf.Max(entry.delayPerform, 0.0001f);

                switch (entry.hitboxType)
                {
                    case HitboxType.Box:
                        main.startSizeX = entry.boxSize.x;
                        main.startSizeY = entry.boxSize.y;
                        main.startSizeZ = 0;
                        break;
                    case HitboxType.Sphere:
                    case HitboxType.Capsule:
                    case HitboxType.None:
                        break;
                }
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
                
                _playToken = token;
                playFeedback?.PlayFeedbacks();
                Perform();
                
                var moveTask = ShouldMoveByDestination()
                    ? MoveByDestinationNodes(token)
                    : UniTask.CompletedTask;

                if (debug) Debug.Log("Deleting");
                var releaseTask = UniTask.Delay(TimeSpan.FromSeconds(deletetime), cancellationToken: token);
                await UniTask.WhenAll(moveTask, releaseTask);

                _pool?.Release(this);
            }
            catch (OperationCanceledException)
            {
                if (debug) Debug.Log("Play Cancelled");
            }
        }

        public void CancelPlay()
        {
            _cts?.Cancel();
        }

        public abstract UniTask PlayPreview();
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

        public void ClearVFX(bool includeChildren = true)
        {
            try
            {
                _cts?.Cancel();
                if (notifyFeedback != null)
                {
                    notifyFeedback.StopFeedbacks();
                    notifyFeedback.RestoreInitialValues();
                }

                if (playFeedback != null)
                {
                    playFeedback.StopFeedbacks();
                    playFeedback.RestoreInitialValues();
                }
                
                if (previewEffect != null)
                {
                    var main = previewEffect.main;
                    main.simulationSpeed = 1f;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                    previewEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    previewEffect.Clear(true);
                    var em = previewEffect.emission;
                    em.enabled = false;
                }
            }
            catch (MissingReferenceException)
            {
            }
        }

        protected virtual Vector3 GetDirectionVector(DestinationDirection direction)
        {
            return direction switch
            {
                DestinationDirection.Left      => Vector3.left,
                DestinationDirection.Right     => Vector3.right,
                DestinationDirection.Up        => Vector3.up,
                DestinationDirection.Down      => Vector3.down,
                DestinationDirection.UpLeft    => (Vector3.left + Vector3.up).normalized,
                DestinationDirection.UpRight   => (Vector3.right + Vector3.up).normalized,
                DestinationDirection.DownLeft  => (Vector3.left + Vector3.down).normalized,
                DestinationDirection.DownRight => (Vector3.right + Vector3.down).normalized,
                _ => Vector3.zero
            };
        }
        
        protected virtual bool ShouldMoveByDestination()
        {
            return currentEntry != null
                   && currentEntry.enableDestination
                   && currentEntry.moveFollowDestination
                   && currentEntry.destinationList != null
                   && currentEntry.destinationList.Count > 0;
        }

        private async UniTask MoveByDestinationNodes(CancellationToken token)
        {
            for (int i = 0; i < currentEntry.destinationList.Count; i++)
            {
                var node = currentEntry.destinationList[i];
                var dir = GetDirectionVector(node.direction);
                if (dir == Vector3.zero || node.distance <= 0f) continue;
                var startPos = transform.position;
                var targetPos = startPos + (dir * node.distance);
                while (Vector3.Distance(transform.position, targetPos) > 0.05f)
                {
                    token.ThrowIfCancellationRequested();
                    transform.position = Vector3.MoveTowards(
                        transform.position, targetPos, node.speed * Time.deltaTime);
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
        }
    }
}