using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class CursorCrosshairController : MonoBehaviour
    {
        [Header("Follow Settings")]
        [SerializeField] private bool hideHardwareCursor = true;
        [SerializeField, Min(0f)] private float followSmoothTime = 0f;
        [SerializeField] private bool useUnscaledTime = true;
        [Tooltip("ถ้าเปิด จะบังคับ anchor/pivot = (0.5,0.5) เพื่อตำแหน่ง/หมุนที่เสถียร")]
        [SerializeField] private bool forceCenterAnchorPivot = true;

        [Header("References")]
        [Tooltip("กล้องของ Canvas (เว้นว่างได้ ถ้าเป็น ScreenSpaceOverlay)")]
        [SerializeField] private Camera canvasCamera;
        [Tooltip("ตัวลูกที่ใช้ทำเอฟเฟกต์หมุน/ย่อ-ขยาย (ถ้าเว้นว่างจะใช้ตัวเอง)")]
        [SerializeField] private RectTransform visual;

        [Header("Input (New Input System)")]
        [SerializeField] private InputActionReference pointerPositionAction;
        [SerializeField] private InputActionReference clickAction;

        [Header("Click Feedback (DOTween)")]
        [SerializeField, Min(0f)] private float clickScale = 0.85f;
        [SerializeField, Min(0f)] private float scaleInDuration = 0.06f;
        [SerializeField, Min(0f)] private float scaleOutDuration = 0.12f;
        [SerializeField, Min(0f)] private float spinDuration = 0.20f;
        [SerializeField] private int spinDirection = 1;
        [SerializeField, Min(0f)] private float feedbackCooldown = 0.05f;

        private RectTransform _rect;                // ตัวที่ใช้ "ขยับตำแหน่ง"
        private RectTransform _visual;              // ตัวที่ใช้ "หมุน/ย่อ-ขยาย"
        private Vector2 _vel;
        private Sequence _seq;
        private float _lastFxTime = -999f;
        private Vector3 _startScale;
        private Quaternion _startRot;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _visual = visual ? visual : _rect;

            if (forceCenterAnchorPivot)
            {
                ForceCenter(_rect);
                ForceCenter(_visual);
            }

            _startScale = _visual.localScale;
            _startRot   = _visual.localRotation;
        }

        private void OnEnable()
        {
            if (hideHardwareCursor)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Confined;
            }

            if (pointerPositionAction) pointerPositionAction.action.Enable();
            if (clickAction)
            {
                clickAction.action.Enable();
                clickAction.action.performed += OnClick;
            }
        }

        private void OnDisable()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (clickAction) clickAction.action.performed -= OnClick;

            KillFx(complete: true);
            ResetVisual();
        }

        private void Update()
        {
            // 1) อ่านตำแหน่ง pointer
            Vector2 screenPos;
            if (pointerPositionAction && pointerPositionAction.action.enabled)
                screenPos = pointerPositionAction.action.ReadValue<Vector2>();
            else if (Mouse.current != null)
                screenPos = Mouse.current.position.ReadValue();
            else
                return;

            // 2) แปลงจอ -> local ของ "พาเรนต์ของ crosshair" เพื่อให้ anchoredPosition ตรง
            var parentRect = _rect.parent as RectTransform;
            if (parentRect == null) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, canvasCamera, out var local))
            {
                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                if (followSmoothTime <= 0f)
                    _rect.anchoredPosition = local;
                else
                    _rect.anchoredPosition = Vector2.SmoothDamp(_rect.anchoredPosition, local, ref _vel, followSmoothTime, Mathf.Infinity, dt);
            }

            // Fallback คลิกจาก Mouse ถ้าไม่มี action
            if (!clickAction && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                TryPlayFx();
        }

        private void OnClick(InputAction.CallbackContext ctx)
        {
            if (ctx.performed) TryPlayFx();
        }

        private void TryPlayFx()
        {
            float now = useUnscaledTime ? Time.unscaledTime : Time.time;
            if (now - _lastFxTime < feedbackCooldown) return;
            _lastFxTime = now;

            // กันทับ: จบอันเก่าให้เรียบร้อย + รีเซ็ตทรง
            KillFx(complete: true);
            ResetVisual();

            // หมุนแกน Z ของ "ตัว visual" เท่านั้น
            float dir = Mathf.Sign(spinDirection == 0 ? 1 : spinDirection);
            Vector3 spinTarget = _visual.localEulerAngles + new Vector3(0, 0, 360f * dir);

            _seq = DOTween.Sequence().SetUpdate(useUnscaledTime);
            _seq.Append(_visual.DOScale(_startScale * clickScale, scaleInDuration).SetEase(Ease.OutQuad));
            _seq.Join(_visual.DOLocalRotate(spinTarget, spinDuration, RotateMode.FastBeyond360).SetEase(Ease.OutCubic));
            _seq.Append(_visual.DOScale(_startScale, scaleOutDuration).SetEase(Ease.OutQuad));
            _seq.OnComplete(ResetVisual);
            _seq.SetLink(_visual.gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void KillFx(bool complete)
        {
            if (_seq != null && _seq.IsActive())
            {
                _seq.Kill(complete);
                _seq = null;
            }
        }

        private void ResetVisual()
        {
            _visual.localScale = _startScale;
            _visual.localRotation = _startRot; // กลับมาองศาเดิมเป๊ะ ๆ
        }

        private static void ForceCenter(RectTransform r)
        {
            if (!r) return;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchorMin = new Vector2(0.5f, 0.5f);
            r.anchorMax = new Vector2(0.5f, 0.5f);
        }
    }
}
