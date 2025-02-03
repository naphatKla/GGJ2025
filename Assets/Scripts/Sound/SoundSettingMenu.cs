using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundSettingMenu : MonoBehaviour
{
    [SerializeField] private GameObject _settingUI;
    [SerializeField] private bool toggle = false;
    private void Awake()
    {
        if (FindObjectsOfType<SoundSettingMenu>().Length > 1)
        {
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        _settingUI.gameObject.SetActive(false);
        toggle = false;
    }

    private void Update()
    {
        if (toggle && Time.timeScale == 1)
        {
            Time.timeScale = 1;
            toggle = false;
            _settingUI.gameObject.SetActive(false);
        }
    }
    
    public void ActivateSetting()
    {
        if (toggle)
        {
            Time.timeScale = 1;
            toggle = false;
            _settingUI.gameObject.SetActive(false);
        }
        else
        {
            Time.timeScale = 0;
            toggle = true;
            _settingUI.gameObject.SetActive(true);
        }
    }
}
