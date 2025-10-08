using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameControl.EventMap
{
    public class PullingCircleMapEvent : BaseMapEvent, ISphereHitbox
    {
        [SerializeField] private Transform firePoint;

        [Header("Hitbox Settings (Sphere)")]
        [SerializeField] private float sphereRadius = 3f;
        [SerializeField] private Vector3 sphereOffset = Vector3.zero;
        [SerializeField] private LayerMask hitLayer;

        private static readonly Collider2D[] _hits = new Collider2D[64];
        public HitboxType HitboxType => HitboxType.Sphere;
        public float Radius { get => sphereRadius; set => sphereRadius = value; }
        public Vector3 Offset { get => sphereOffset; set => sphereOffset = value; }

        public override async UniTask PlayPreview()
        {
            if (previewEffect == null) return;

            previewEffect.Play();
            notifyFeedback?.PlayFeedbacks();
            await UniTask.WaitWhile(
                () => previewEffect != null && previewEffect.IsAlive(true), 
                cancellationToken: this.GetCancellationTokenOnDestroy()
            );

        }

        protected override void Perform()
        {
            
        }

        private void OnDrawGizmos()
        {
            if (firePoint == null) return;

            var center = firePoint.TransformPoint(sphereOffset);
            
            Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
            Gizmos.DrawWireSphere(center, sphereRadius);
        }
    }
}
