using UnityEngine;
using Sirenix.OdinInspector;

namespace Tools
{
    #if UNITY_EDITOR
    /// <summary>
    /// Minimal 2D Gizmos Debugger (Odin)
    /// - Circle / Rect (only shows fields for the active shape)
    /// - Start position + Offset (World/Local)
    /// - Custom color
    /// - Show only when selected (typical) or always
    /// </summary>
    public class GizmosDebugger : MonoBehaviour
    {
        public enum Shape2D { Circle, Rect }
        public enum SpaceMode { World, Local }

        // --- Display ---
        [EnumToggleButtons, LabelText("Shape")]
        public Shape2D shape = Shape2D.Circle;

        [ToggleLeft, LabelText("Show Only When Selected")]
        public bool showOnlyWhenSelected = true;

        [LabelText("Color")]
        public Color gizmoColor = new Color(0.2f, 0.9f, 1f, 1f);

        // --- Position ---
        [PropertySpace(4)]
        [EnumToggleButtons, LabelText("Start Space")]
        public SpaceMode startSpace = SpaceMode.World;

        [LabelText("Start Pos (XY)")]
        public Vector2 startPos;

        [EnumToggleButtons, LabelText("Offset Space")]
        public SpaceMode offsetSpace = SpaceMode.Local;

        [LabelText("Offset (XY)")]
        public Vector2 offset;

        // --- Circle-only ---
        [ShowIf("@shape == Shape2D.Circle")]
        [MinValue(0f), LabelText("Radius")]
        public float radius = 1f;

        [ShowIf("@shape == Shape2D.Circle")]
        [Range(3, 128), LabelText("Segments")]
        public int segments = 32;

        // --- Rect-only ---
        [ShowIf("@shape == Shape2D.Rect")]
        [LabelText("Size (W,H)")]
        public Vector2 rectSize = Vector2.one;

        [ShowIf("@shape == Shape2D.Rect")]
        [ToggleLeft, LabelText("Wireframe")]
        public bool wireRect = true;

        // -------- Draw --------
        private void OnDrawGizmos()
        {
            if (!showOnlyWhenSelected) Draw();
        }

        private void OnDrawGizmosSelected()
        {
            if (showOnlyWhenSelected) Draw();
        }

        private void Draw()
        {
            Gizmos.color = gizmoColor;

            // World center on XY; keep Z at this object's Z so it appears at the same depth.
            var baseZ = transform.position.z;
            var startWorld = startSpace == SpaceMode.Local
                ? transform.TransformPoint(new Vector3(startPos.x, startPos.y, 0f))
                : new Vector3(startPos.x, startPos.y, baseZ);

            var worldOffset = offsetSpace == SpaceMode.Local
                ? transform.TransformVector(new Vector3(offset.x, offset.y, 0f))
                : new Vector3(offset.x, offset.y, 0f);

            var center = startWorld + worldOffset;

            if (shape == Shape2D.Circle)
            {
                DrawWireCircle2D(center, radius, segments);
                return;
            }

            // Rect (rotate only if offset is in Local space)
            var rot = offsetSpace == SpaceMode.Local ? transform.rotation : Quaternion.identity;
            var old = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(center, rot, Vector3.one);

            var size3 = new Vector3(rectSize.x, rectSize.y, 0.001f);
            if (wireRect) Gizmos.DrawWireCube(Vector3.zero, size3);
            else Gizmos.DrawCube(Vector3.zero, size3);

            Gizmos.matrix = old;
        }

        // -------- Helpers --------
        private void DrawWireCircle2D(Vector3 center, float r, int seg)
        {
            if (r <= 0f || seg < 3) return;

            float step = Mathf.PI * 2f / seg;
            Vector3 prev = center + new Vector3(r, 0f, 0f);

            for (int i = 1; i <= seg; i++)
            {
                float a = step * i;
                Vector3 next = center + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }

        private void OnValidate()
        {
            radius = Mathf.Max(0f, radius);
            segments = Mathf.Clamp(segments, 3, 128);
            rectSize = new Vector2(Mathf.Max(0f, rectSize.x), Mathf.Max(0f, rectSize.y));
        }
    }
    #endif
}
