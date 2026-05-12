using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Manager;
using UnityEngine;

namespace GameControl.EventMap
{
    public class ZoneMapEvent : BaseMapEvent, ISphereHitbox, IFixedUpdateable
    {
        
        [SerializeField] private Transform firePoint;

        [Header("Hitbox Settings (Sphere)")]
        [SerializeField] private float sphereRadius = 6f;
        [SerializeField] private Vector2 sphereOffset = Vector2.zero;
        [SerializeField] private LayerMask hitLayer;
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
        public void OnFixedUpdate()
        {
            if (!IsPerforming) return;
            //Dmg on touch
            DamageOnTouch();
        }
        

        private void DamageOnTouch()
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
                CombatManager.ApplyRawDamageTo(go, gameObject, mapEventId, damage);
            }
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
