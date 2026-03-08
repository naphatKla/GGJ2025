using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Manager;
using UnityEngine;

namespace GameControl.EventMap
{
    public class PushCircleMapEvent : BaseMapEvent, ISphereHitbox, IFixedUpdateable
    {
        [SerializeField] private Transform firePoint;
        
        [Header("Hitbox Settings (Sphere)")]
        [SerializeField] private float sphereRadius = 6f;
        [SerializeField] private Vector2 sphereOffset = Vector2.zero;
        [SerializeField] private LayerMask hitLayer;

        [Header("Push Settings")]
        [SerializeField] float pushDistance = 8f;
        [SerializeField] float pushDuration = 0.5f;
        private bool IsPerforming;

        public HitboxType HitboxType => HitboxType.Sphere;
        public float Radius { get => sphereRadius; set => sphereRadius = value; }
        public Vector3 Offset { get => sphereOffset; set => sphereOffset = value; }
        
        private void OnEnable() => FixedUpdateManager.Instance.Register(this);
        private void OnDisable()
        {
            FixedUpdateManager.Current?.Unregister(this);
            IsPerforming = false;
        }

        public  override async UniTask PlayPreview()
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
            //Pull
            PushObject();
        }

        private void PushObject()
        {
            Vector2 center = firePoint ? firePoint.TransformPoint(sphereOffset) : (Vector2)transform.TransformPoint(sphereOffset);
            var targetsInRange = Physics2D.OverlapCircleAll(center, sphereRadius, hitLayer);
            foreach (var target in targetsInRange)
            {
                Vector2 pushDirection = target.transform.position - firePoint.transform.position;
                Vector2 pushBackDestination = (Vector2)target.transform.position + pushDirection.normalized * pushDistance;
                
                CombatManager.TryGetCharacterFromCache(target.gameObject, out var targetController);
                targetController.MovementSystem.TryMoveToPositionOverTime(pushBackDestination, pushDuration);
                CombatManager.ApplyRawDamageTo(target.gameObject, gameObject, mapEventId, damage);
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