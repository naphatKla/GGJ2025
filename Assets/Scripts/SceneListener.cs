using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneListener : MonoBehaviour
{
    [SerializeField] private GameObject fader;
    private void Start()
    {
        GlobalSceneEvents.OnSceneReady += PlayFade;
        Debug.Log("Add listener");
    }

    private void OnDisable()
    {
        GlobalSceneEvents.OnSceneReady -= PlayFade;
    }

    private void PlayFade()
    {
        fader.SetActive(true);
        Debug.Log("FadeIn");
    }
}
