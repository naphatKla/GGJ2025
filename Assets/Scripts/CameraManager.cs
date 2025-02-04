using System;
using Cinemachine;
using DG.Tweening;
using MoreMountains.Tools;
using UnityEngine;

public class CameraManager : MMSingleton<CameraManager>
{
    public CinemachineVirtualCamera currentCamera;
    private float _startOrthographicSize;
    private Tween _lensTween;

    
    private void Start()
    {
        _startOrthographicSize = currentCamera.m_Lens.OrthographicSize;
    }
    
    public void SetLensOrthographicSize(float size, float duration)
    {
        if (_lensTween.IsActive()) _lensTween.Kill();
        _lensTween = DOTween.To(() => currentCamera.m_Lens.OrthographicSize, x => currentCamera.m_Lens.OrthographicSize = x, size, duration);
    }

    public void ResetLensOrthographicSize()
    {
        SetLensOrthographicSize(_startOrthographicSize,0.3f);
    }
}
