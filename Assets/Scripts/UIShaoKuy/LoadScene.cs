using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class LoadScene : MonoBehaviour
{
    [FormerlySerializedAs("soundSetting")] [Header("UI")]
    public GameObject openUI;
    
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
        if (openUI != null)
        {
            openUI.SetActive(!openUI.activeSelf);
        }
    }
}
