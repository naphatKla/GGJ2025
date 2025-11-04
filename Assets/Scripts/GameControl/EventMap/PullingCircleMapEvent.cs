using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Characters.MovementSystems;
using Manager;

namespace GameControl.EventMap
{
    public class PullingCircleMapEvent : BaseMapEvent, ISphereHitbox, IFixedUpdateable
    {
        [SerializeField] private Transform firePoint;

        [Header("Hitbox Settings (Sphere)")]
        [SerializeField] private float sphereRadius = 6f;
        [SerializeField] private Vector2 sphereOffset = Vector2.zero;
        [SerializeField] private LayerMask hitLayer;

        [Header("Pull Settings")]
        [SerializeField] private float dmgCenterRadius = 1f;
        [SerializeField] float basePull = 8f; 
        [SerializeField] float stopDistance = 0.1f;
        [SerializeField] AnimationCurve pullCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] bool useExponential = true;
        private bool IsPerforming;

        private static readonly Collider2D[] _hits = new Collider2D[64];

        public HitboxType HitboxType => HitboxType.Sphere;
        public float Radius { get => sphereRadius; set => sphereRadius = value; }
        public Vector3 Offset { get => sphereOffset; set => sphereOffset = value; }
        
        private void OnEnable() => FixedUpdateManager.Instance.Register(this);
        private void OnDisable()
        {
            FixedUpdateManager.Current?.Unregister(this);
            IsPerforming = false;
        }

        public override async UniTask PlayPreview()
        {
            if (previewEffect == null) return;
            previewEffect?.Play();
            notifyFeedback?.PlayFeedbacks();
            await UniTask.WaitWhile(
                () => previewEffect != null && previewEffect.IsAlive(true),
                cancellationToken: this.GetCancellationTokenOnDestroy()
            );
        }

        protected override void Perform()
        {
            IsPerforming = true;
        }

        private void OnDrawGizmos()
        {
            if (firePoint == null) return;
            var center = firePoint.TransformPoint(sphereOffset);
            Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
            Gizmos.DrawWireSphere(center, sphereRadius);
            
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawSphere(center, dmgCenterRadius);
        }

        public void OnFixedUpdate()
        {
            if (!IsPerforming) return;
            //Pull
            PullPlayer();
            //Dmg on touch Center
            DamageOnTouch();
            
        }

        private void DamageOnTouch()
        {
            Vector2 center = firePoint ? firePoint.TransformPoint(sphereOffset) : (Vector2)transform.TransformPoint(sphereOffset);
            var count = Physics2D.OverlapCircleNonAlloc(center, dmgCenterRadius, _hits, hitLayer);
            if (count == 0) return;

            for (var i = 0; i < count; i++)
            {
                var col = _hits[i];
                if (!col) continue;

                var go = col.gameObject;
                if (go == gameObject) continue;
                CombatManager.ApplyRawDamageTo(go, gameObject, damage);
            }
        }

        private void PullPlayer()
        {
            Vector2 center = firePoint ? firePoint.TransformPoint(sphereOffset) : (Vector2)transform.TransformPoint(sphereOffset);
            var count = Physics2D.OverlapCircleNonAlloc(center, sphereRadius, _hits, hitLayer);
            if (count == 0) return;

            for (var i = 0; i < count; i++)
            {
                var col = _hits[i];
                if (!col) continue;

                var go = col.gameObject;
                if (go == gameObject) continue;

                var mover = go.GetComponent<BaseMovementSystem>();
                if (mover == null) continue;

                Vector2 pos = mover.transform.position;
                var toCenter = center - pos;
                var dist = toCenter.magnitude;
                if (dist <= stopDistance) continue;
                var proximity = Mathf.InverseLerp(sphereRadius, 0f, dist);
                var curveMul = Mathf.Max(0f, pullCurve.Evaluate(proximity));
                var strength = basePull * curveMul;

                var dir = toCenter / dist;

                if (!useExponential)
                {
                    var step = strength * Time.fixedDeltaTime;
                    var nextPos = pos + dir * Mathf.Min(step, dist - stopDistance);
                    mover.TryMoveRawPosition(nextPos);
                }
                else
                {
                    var t = 1f - Mathf.Exp(-strength * Time.fixedDeltaTime);
                    var lerped = Vector2.Lerp(pos, center, t);
                    if ((lerped - center).sqrMagnitude < stopDistance * stopDistance)
                        lerped = center + (pos - center).normalized * stopDistance;

                    mover.TryMoveRawPosition(lerped);
                }
            }
        }
    }
}
