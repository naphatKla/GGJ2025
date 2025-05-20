using System.Collections.Generic;
using UnityEngine;

namespace ProjectExtensions
{
    /// <summary>
    /// Utility class for snapping 2D colliders (circle, box) to match the visual bounds or physics shape of a sprite.
    /// Useful for dynamic sprite assignment without needing separate prefab colliders.
    /// </summary>
    public static class Collider2DSnapper
    {
        #region CircleCollider2D

        /// <summary>
        /// Resize a CircleCollider2D based on the sprite's visual bounds (includes transparent area).
        /// </summary>
        public static void Snap(SpriteRenderer renderer, CircleCollider2D collider)
        {
            if (renderer.sprite == null) return;

            var bounds = renderer.sprite.bounds;
            collider.offset = bounds.center;
            collider.radius = Mathf.Max(bounds.extents.x, bounds.extents.y);
        }

        /// <summary>
        /// Resize a CircleCollider2D based on the tight-fitting physics shape of the sprite (ignores full-transparent pixels).
        /// </summary>
        public static void SnapPhysicsShape(SpriteRenderer renderer, CircleCollider2D collider)
        {
            if (renderer.sprite == null) return;

            var shape = new List<Vector2>();
            renderer.sprite.GetPhysicsShape(0, shape);
            if (shape.Count == 0) return;

            float maxRadius = 0f;
            foreach (var point in shape)
            {
                float dist = point.magnitude;
                if (dist > maxRadius) maxRadius = dist;
            }

            collider.offset = renderer.sprite.bounds.center;
            collider.radius = maxRadius;
        }

        #endregion

        #region BoxCollider2D

        /// <summary>
        /// Resize a BoxCollider2D to fit the sprite's visual bounds (includes transparent area).
        /// </summary>
        public static void Snap(SpriteRenderer renderer, BoxCollider2D collider)
        {
            if (renderer.sprite == null) return;

            var bounds = renderer.sprite.bounds;
            collider.offset = bounds.center;
            collider.size = bounds.size;
        }

        /// <summary>
        /// Resize a BoxCollider2D to fit the tightest rectangle bounding the physics shape.
        /// </summary>
        public static void SnapPhysicsShape(SpriteRenderer renderer, BoxCollider2D collider)
        {
            if (renderer.sprite == null) return;

            var shape = new List<Vector2>();
            renderer.sprite.GetPhysicsShape(0, shape);
            if (shape.Count == 0) return;

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            foreach (var p in shape)
            {
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
            }

            Vector2 size = new Vector2(maxX - minX, maxY - minY);
            Vector2 center = new Vector2(minX + size.x / 2f, minY + size.y / 2f);

            collider.offset = center;
            collider.size = size;
        }

        #endregion
    }
}
