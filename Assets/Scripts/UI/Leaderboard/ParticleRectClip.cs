using UnityEngine;
using UnityEngine.UI;

namespace UI.Leaderboard
{
    /// <summary>
    /// ตัด ParticleSystem ให้หายตรงขอบ Viewport ของ ScrollView แบบเนียนๆ (Mask / RectMask2D ตัดได้แค่ UI ไม่ตัด Particle)
    /// ส่งกรอบ Viewport ไปให้ Shader ผ่าน MaterialPropertyBlock ทุกเฟรม
    /// ใช้ได้กับ Shader ที่รองรับ _VfxClipEnabled / _VfxClipMatrix / _VfxClipRect
    /// (ตอนนี้: SH_VFX_SimplePremult, SH_VFX_PanDissolvePremult)
    /// ใส่ที่ root ของ Cell ที่มี VFX (เช่น LeaderboardCell 1-3)
    /// </summary>
    [DisallowMultipleComponent]
    public class ParticleRectClip : MonoBehaviour
    {
        [Tooltip("กรอบที่ใช้ตัด ถ้าว่างจะหา RectMask2D / Mask ตัวแม่ที่ใกล้ที่สุดเอง")]
        [SerializeField] private RectTransform viewport;

        [Tooltip("ตัดแนวตั้ง (ขอบบน/ล่างของ ScrollView)")]
        [SerializeField] private bool clipVertical = true;

        [Tooltip("ตัดแนวนอน (ขอบซ้าย/ขวา) - ปิดไว้ให้ VFX ล้นออกด้านข้างได้เหมือนเดิม")]
        [SerializeField] private bool clipHorizontal;

        private static readonly int ClipEnabledId = Shader.PropertyToID("_VfxClipEnabled");
        private static readonly int ClipMatrixId = Shader.PropertyToID("_VfxClipMatrix");
        private static readonly int ClipRectId = Shader.PropertyToID("_VfxClipRect");
        private const float Unbounded = 1e7f;

        private ParticleSystemRenderer[] _renderers;
        private MaterialPropertyBlock _mpb;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<ParticleSystemRenderer>(true);
            _mpb = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (viewport == null) viewport = FindViewport();
        }

        private void OnDisable()
        {
            // คืนค่าเดิม (ไม่ตัด) เผื่อ Cell ถูกเอาไปใช้ที่อื่น
            foreach (var r in _renderers)
                if (r) r.SetPropertyBlock(null);
        }

        // ถูกย้าย parent ตอน pool (LoopScrollRect) -> หา viewport ใหม่
        private void OnTransformParentChanged()
        {
            viewport = FindViewport();
        }

        private void LateUpdate()
        {
            if (viewport == null)
            {
                OnDisable();
                return;
            }

            var rect = viewport.rect;
            var clipRect = new Vector4(
                clipHorizontal ? rect.xMin : -Unbounded,
                clipVertical ? rect.yMin : -Unbounded,
                clipHorizontal ? rect.xMax : Unbounded,
                clipVertical ? rect.yMax : Unbounded);

            foreach (var r in _renderers)
            {
                if (!r) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetFloat(ClipEnabledId, 1f);
                _mpb.SetMatrix(ClipMatrixId, viewport.worldToLocalMatrix);
                _mpb.SetVector(ClipRectId, clipRect);
                r.SetPropertyBlock(_mpb);
            }
        }

        private RectTransform FindViewport()
        {
            for (var t = transform.parent; t != null; t = t.parent)
                if (t.GetComponent<RectMask2D>() || t.GetComponent<Mask>())
                    return (RectTransform)t;
            return null;
        }
    }
}
