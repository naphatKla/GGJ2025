using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class SoundSettingMenu : MonoBehaviour
{
    [SerializeField] private GameObject _settingUI;
    private bool toggle = false;

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
        _settingUI.SetActive(false);
        toggle = false;
    }

    public void ActivateSetting()
    {
        if (toggle) return;

        _settingUI.SetActive(true);
        toggle = true;

        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            Time.timeScale = 0;
        }
        
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void DeactivateSetting()
    {
        if (!toggle) return;

        _settingUI.SetActive(false);
        toggle = false;

        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            Time.timeScale = 1;
        }

        // Reset Focus (กันปุ่ม Setting ค้าง hover)
        EventSystem.current.SetSelectedGameObject(null);
    }
}