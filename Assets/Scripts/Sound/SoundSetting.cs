using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundSetting : MonoBehaviour
{
    public GameObject soundSetting;
    private bool _isActive;

    private void Start()
    {
        /*soundSetting.gameObject.SetActive(false);*/
        _isActive = false;
    }

    public void OpenUI()
    {
        if (soundSetting != null)
        {
            soundSetting.SetActive(!soundSetting.activeSelf);
        }
    }
}
