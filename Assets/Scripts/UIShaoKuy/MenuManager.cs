using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    private SoundSettingMenu settingUI;
    [SerializeField] private GameObject soundButton;
    void Start()
    {
        FindSettingMenu();
        if (settingUI == null)  { return; }
        if (soundButton != null)
        {
            soundButton.GetComponent<Button>().onClick.AddListener(settingUI.ActivateSetting);
        }
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            AudioManager.Instance.PlayAudio("Music");
        }
        else
        {
            AudioManager.Instance.StopAudio("Music");
        }
    }

    private void FindSettingMenu()
    {
        settingUI = FindObjectOfType<SoundSettingMenu>();
    }
}
