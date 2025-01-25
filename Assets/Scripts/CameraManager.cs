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

    public void SetLensOrthographicSize(float size, float duration)
    {
        Debug.LogWarning("CALL");
        DOTween.To(() => currentCamera.m_Lens.OrthographicSize, x => currentCamera.m_Lens.OrthographicSize = x, size, duration);
    }
}
