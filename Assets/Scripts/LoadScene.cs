using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
    [Header("UI")]
    public GameObject soundSetting;
    
    public void StartGame()
    {
        SceneManager.LoadScene("Gameplay");
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void Credits()
    {
        SceneManager.LoadScene("Credits");
    }

    public void Resume()
    {
        //Back to game
    }

    public void Exit()
    {
        Application.Quit();
    }

    public void OpenSoundSetting()
    {
        if (soundSetting != null)
        {
            soundSetting.SetActive(!soundSetting.activeSelf);
        }
    }
}
