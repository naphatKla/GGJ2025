using System;
using Cysharp.Threading.Tasks;
using Manager;
using UnityEngine;

namespace GameControl.EventMap
{
    public class ProjectileMapEvent : BaseMapEvent, IBoxHitbox
    {
        [SerializeField] private Transform firePoint;
        [SerializeField] private ParticleSystem previewEffect;
        
        [Header("Hitbox Settings")]
        [SerializeField] private Vector3 hitboxSize = new Vector3(2f, 80f, 0);
        [SerializeField] private Vector3 hitboxOffset = new Vector3(0, 0f, 0);
        [SerializeField] private LayerMask hitLayer;

        public HitboxType HitboxType => HitboxType.Box;
        public Vector3 Size { get => hitboxSize; set => hitboxSize = value; }
        public Vector3 Offset { get => hitboxOffset; set => hitboxOffset = value; }

        private void OnValidate()
        {
            ApplySizeToEffect();
        }

        private void ApplySizeToEffect()
        {
            if (previewEffect != null)
            {
                var main = previewEffect.main;
                main.startSizeX = hitboxSize.x;
                main.startSizeY = hitboxSize.y;
                main.startSizeZ = hitboxSize.z;
            }
        }

        public override async UniTask PlayPreview()
        {
            ApplySizeToEffect();
            previewEffect?.Play();
        }

        protected override void Perform()
        {
            Vector3 pos = hitboxOffset + new Vector3(0, hitboxSize.y / 2, 0);
            Vector3 center = firePoint.TransformPoint(pos);

            Collider[] hits = Physics.OverlapBox(center, hitboxSize * 0.5f, firePoint.rotation, hitLayer);

            foreach (var hit in hits) CombatManager.ApplyRawDamageTo(hit.gameObject, damage);
        }
        
        private void OnDrawGizmos()
        {
            if (firePoint == null) return;

            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);

            Vector3 pos = hitboxOffset + new Vector3(0, hitboxSize.y / 2, 0);
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