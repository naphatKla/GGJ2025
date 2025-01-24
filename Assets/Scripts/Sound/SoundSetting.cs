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
        soundSetting.gameObject.SetActive(false);
        _isActive = false;
    }

    public void OpenUI()
    {
        if (_isActive == false)
        {
            soundSetting.gameObject.SetActive(true);
            _isActive = true;
        }
        else if (_isActive == true)
        {
            soundSetting.gameObject.SetActive(false);
            _isActive = false;
        }
    }
}
