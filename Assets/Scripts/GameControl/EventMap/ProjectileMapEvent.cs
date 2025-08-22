using System;
using Cysharp.Threading.Tasks;
using Manager;
using UnityEngine;

namespace GameControl.EventMap
{
    public class ProjectileMapEvent : BaseMapEvent, IBoxHitbox
    {
        [SerializeField] private Transform firePoint;

        [Header("Hitbox Settings")] [SerializeField] private Vector2 hitboxSize = new Vector3(2f, 80f);
        [SerializeField] private Vector2 hitboxOffset = new Vector3(0, 0f);
        [SerializeField] private LayerMask hitLayer;

        private static readonly Collider2D[] _hits = new Collider2D[64];
        public HitboxType HitboxType => HitboxType.Box;
        public Vector3 Size { get => hitboxSize; set => hitboxSize = value; }
        public Vector3 Offset { get => hitboxOffset; set => hitboxOffset = value; }

        private void OnValidate()
        {
            var main = previewEffect.main;
            main.startSizeX = hitboxSize.x;
            main.startSizeY = hitboxSize.y;
            main.startSizeZ = 0;
        }

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
            Vector3 pos = hitboxOffset + new Vector2(0, hitboxSize.y / 2);
            Vector3 center = firePoint.TransformPoint(pos);
            
            var size = Physics2D.OverlapBoxNonAlloc(center, hitboxSize, transform.eulerAngles.z, _hits, hitLayer);
            
            for (int i = 0; i < size; i++)
                CombatManager.ApplyRawDamageTo(_hits[i].gameObject, damage);
        }
        
        private void OnDrawGizmos()
        {
            if (firePoint == null) return;

            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);

            Vector3 pos = hitboxOffset + new Vector2(0, hitboxSize.y / 2);
            Vector3 center = firePoint.TransformPoint(pos);
            
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(
                center,
                firePoint.rotation,
                Vector3.one
            );
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawCube(Vector3.zero, hitboxSize);
        }
    }
}