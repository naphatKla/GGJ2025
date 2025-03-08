using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class LoadScene : MonoBehaviour
{
    [Header("UI")]
    public GameObject pauseUI;
    public GameObject loseUI;

    [Header("Tutorial")]
    public List<GameObject> tutorialSteps;
    private int currentStep;
    private bool isTutorialRunning;
    private bool hasStarted;
    private static bool tutorialCompleted; // เพิ่มตัวแปรป้องกันการ Reset

    [Header("Cutscene Settings")]
    public float cutsceneDelay = 2f;

    void Start()
    {
        pauseUI.SetActive(false);
    }
    

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Backslash))
        {
            currentStep = 0;
            tutorialCompleted = false;
        }
        
        if (isTutorialRunning && Input.GetMouseButtonDown(0))
        {
            NextTutorialStep();
        }

        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (Time.timeScale == 0 && hasStarted)
        {
            ResumeGame();
        }
        else if (hasStarted)
        {
            PauseGame();
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
        Time.timeScale = 1;
        hasStarted = true;
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1;
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

    public void StartTutorials()
    {
        if (tutorialCompleted) return;
        if (tutorialSteps.Count <= 0) return;
        isTutorialRunning = true;
        tutorialCompleted = false;
        ShowTutorialStep(0);
    }
    
    private void ShowTutorialStep(int step)
    {
        if (tutorialCompleted) return;
        Time.timeScale = 0;
        foreach (var stepUI in tutorialSteps)
        {
            stepUI.SetActive(false);
        }

        if (step < tutorialSteps.Count)
        {
            tutorialSteps[step].SetActive(true);
            currentStep = step;
            Debug.Log($"แสดง Tutorial Step {step}"); // Debug
        }
    }

    private void NextTutorialStep()
    {
        if (!isTutorialRunning || tutorialCompleted) return;

        if (currentStep < tutorialSteps.Count - 1)
        {
            ShowTutorialStep(currentStep + 1);
        }
        else
        {
            EndTutorial();
        }
    }

    private void EndTutorial()
    {
        if (!isTutorialRunning) return;

        isTutorialRunning = false;
        tutorialCompleted = true;
        Time.timeScale = 1;
        hasStarted = true;

        // ปิดทุก UI ของ Tutorial
        foreach (var stepUI in tutorialSteps)
        {
            stepUI.SetActive(false);
        }

        Debug.Log("Tutorial จบแล้ว");
    }
}
