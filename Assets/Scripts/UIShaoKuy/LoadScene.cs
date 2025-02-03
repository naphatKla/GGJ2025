using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class LoadScene : MonoBehaviour
{
    [FormerlySerializedAs("soundSetting")] [Header("UI")]
    public GameObject pauseUI;
    public GameObject loseUI;
    
    public GameObject playerControllerUI;
    private bool hasStarted = false;
    

    void Start()
    {
        // เริ่มเกมโดยแสดง Player Controller UI และหยุดเวลา
        playerControllerUI.SetActive(true);
        pauseUI.SetActive(false);
        loseUI.SetActive(false);
        Time.timeScale = 0;
    }

    void Update()
    {
        if (!hasStarted && Input.GetMouseButtonDown(0) && Time.timeScale == 0)
        {
            StartGame();
        }
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Time.timeScale == 0 && hasStarted)
            {
                ResumeGame();
            }
            else if (hasStarted)
            {
                PauseGame();
            }
        }
    }
    
    public void ResumeGame()
    {
        pauseUI.SetActive(false);
         Debug.Log("Resume");
        Time.timeScale = 1;
    }

    public void PauseGame()
    {
        pauseUI.SetActive(true);
        Time.timeScale = 0;
    }
    
    public void StartGame()
    {
        playerControllerUI.SetActive(false);
        Time.timeScale = 1;
        hasStarted = true;
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void Credits()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("Credits");
    }
    
    public void RestartGame()
    {
        SceneManager.LoadScene("Gameplay");
        loseUI.SetActive(false);
    }

    public void Exit()
    {
        Application.Quit();
        Debug.Log("Exit");
    }
    
    public void PlayerDie()
    {
        loseUI.SetActive(true);
        Time.timeScale = 0.5f;
    }

    /*public void OpenSoundSetting()
    {
        StartCoroutine(ToggleUI());
        pauseUI.SetActive(false);
    }*/

    /*private IEnumerator ToggleUI()
    {
        bool wasActive = openUI.activeSelf;
        openUI.SetActive(!wasActive); // สลับสถานะ
        yield return null; // รอ 1 frame เพื่อให้ Unity อัปเดต

        // ตรวจสอบและรีเซ็ต Canvas หากจำเป็น
        Canvas canvas = openUI.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            if (!wasActive) // ถ้าเปิดครั้งแรก
            {
                canvas.enabled = false;
                yield return null; // รออีก 1 frame
                canvas.enabled = true;
            }
        }
    }*/

    /*private IEnumerator DelayDisableUI()
    {
        yield return new WaitForSeconds(0.5f);
        openUI.SetActive(false);
    }*/

}
