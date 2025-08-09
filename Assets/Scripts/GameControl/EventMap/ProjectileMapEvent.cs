using Cysharp.Threading.Tasks;
using Manager;
using UnityEngine;

namespace GameControl.EventMap
{
    public class ProjectileMapEvent : BaseMapEvent
    {
        [SerializeField] private Transform firePoint;
        [SerializeField] private ParticleSystem previewEffect;

        [Header("Hitbox Settings")]
        [SerializeField] private Vector3 hitboxSize = new Vector3(1f, 1f, 1f);
        [SerializeField] private Vector3 hitboxOffset = new Vector3(0f, 0f, 1f);
        [SerializeField] private LayerMask hitLayer;

        public override async UniTask PlayPreview()
        {
            previewEffect?.Play();
        }

        protected override void Perform()
        {
            Vector3 center = firePoint.TransformPoint(hitboxOffset);

            Collider[] hits = Physics.OverlapBox(center, hitboxSize * 0.5f, firePoint.rotation, hitLayer);

            foreach (var hit in hits) 
                CombatManager.ApplyRawDamageTo(hit.gameObject, damage);
        }
        
        private void OnDrawGizmos()
        {
            if (firePoint == null) return;

            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);

            Matrix4x4 rotationMatrix = Matrix4x4.TRS(
                firePoint.TransformPoint(hitboxOffset),
                firePoint.rotation,
                Vector3.one
            );
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawCube(Vector3.zero, hitboxSize);
        }
    }
}