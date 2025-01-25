using System;
using Cinemachine;
using DG.Tweening;
using MoreMountains.Tools;
using UnityEngine;

public class CameraManager : MMSingleton<CameraManager>
{
    public CinemachineVirtualCamera currentCamera;
    public float StartOrthographicSize { get; private set; }
    
    private void Start()
    {
        StartOrthographicSize = currentCamera.m_Lens.OrthographicSize;
    }

    Tween lensTween;
    public void SetLensOrthographicSize(float size, float duration)
    {
        if (lensTween.IsActive()) lensTween.Kill();
        lensTween = DOTween.To(() => currentCamera.m_Lens.OrthographicSize, x => currentCamera.m_Lens.OrthographicSize = x, size, duration);
    }
}
