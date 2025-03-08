using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CursorManager : MonoBehaviour
{
    [SerializeField] private Image cursorUI;
    [SerializeField] private Canvas canvas;
    private RectTransform _canvasRectTransform;
    private Tween _clickTween;

    private void Start()
    {
        _canvasRectTransform = canvas.GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Cursor.visible = true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Cursor.visible = false;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        Cursor.visible = false; 
    }

    private void Update()
    {
        //PlayClickTween();
        UICursorFollowMousePosition();
    }

    private void UICursorFollowMousePosition()
    {
        Vector2 mousePosition = Input.mousePosition;
        
        Vector2 localPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRectTransform ,  // The RectTransform of the Canvas
            mousePosition,                        // The screen position of the mouse
            canvas.worldCamera,                   // Camera used by the Canvas
            out localPosition                      // The resulting local position
        );
        
        cursorUI.rectTransform.anchoredPosition = localPosition;
    }

    private void PlayClickTween()
    {
        if (_clickTween.IsActive()) return;
        if (!Input.GetMouseButtonDown(0)) return;
        _clickTween = cursorUI.transform.DOScale(Vector2.one * 1.35f, 0.25f).SetLoops(2, LoopType.Yoyo);
    }
}
