using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class CursorCrosshairController : MonoBehaviour
    {
        [Header("Follow")]
        [SerializeField] private bool hideHardwareCursor = true;
        [SerializeField] private bool forceCenterAnchorPivot = true;
        
        [Header("Input (New Input System)")]
        [SerializeField] private InputActionReference pointerPositionAction;
        [SerializeField] private InputActionReference clickAction;

        [Header("Feedback")] 
        [SerializeField] private Transform cursorIn;
        [SerializeField] private Transform cursorOut;
        private Tween feedbackTween;
        public event Action OnClicked;
        private RectTransform _rect;
        private Vector2 _vel;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            if (forceCenterAnchorPivot)
            {
                _rect.pivot = new Vector2(0.5f, 0.5f);
                _rect.anchorMin = new Vector2(0.5f, 0.5f);
                _rect.anchorMax = new Vector2(0.5f, 0.5f);
            }
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
                clickAction.action.performed += OnClickPerformed;
            }
        }

        private void OnDisable()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (clickAction) clickAction.action.performed -= OnClickPerformed;
        }

        private void Update()
        {
            Vector2 screenPos;
            if (pointerPositionAction && pointerPositionAction.action.enabled)
                screenPos = pointerPositionAction.action.ReadValue<Vector2>();
            else if (Mouse.current != null)
                screenPos = Mouse.current.position.ReadValue();
            else
                return;
            
            var parentRect = _rect.parent as RectTransform;
            if (!parentRect) return;
            
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, null, out var local))
            {
                _rect.anchoredPosition = local;  
            }
            
            if (!clickAction && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                OnClicked?.Invoke();
        }

        private void OnClickPerformed(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
            {
                OnClicked?.Invoke();
                if (feedbackTween.IsActive()) return;
                feedbackTween = cursorOut.DOScale(new Vector3(1.3f, 1.3f, 1), 0.125f).SetLoops(2, LoopType.Yoyo);
            }
        }
    }
}
