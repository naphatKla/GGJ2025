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
    private int currentStep = 0;
    private bool isTutorialRunning = true;

    private bool hasStarted = false;
    private bool _isCutsceneRun = true;

    [Header("Cutscene Settings")]
    public float cutsceneDelay = 2f;

    void Start()
    {
        pauseUI.SetActive(false);
        
        if (tutorialSteps.Count > 0)
        {
            StartCoroutine(StartTutorialWithDelay());
        }
        else
        {
            isTutorialRunning = false;
        }
    }

    void Update()
    {
        if (isTutorialRunning && Input.GetMouseButtonDown(0))
        {
            NextTutorialStep();
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

    public void Tutorial()
    {
        Time.timeScale = 0;
        _isCutsceneRun = false;
    }

    public void ShowTutorialStep(int step)
    {
        foreach (var stepUI in tutorialSteps)
        {
            stepUI.SetActive(false);
        }
        
        if (step < tutorialSteps.Count)
        {
            tutorialSteps[step].SetActive(true);
            currentStep = step;
        }
        else
        {
            EndTutorial();
        }
    }
    
    public void NextTutorialStep()
    {
        if (currentStep < tutorialSteps.Count - 1)
        {
            ShowTutorialStep(currentStep + 1);
        }
        else
        {
            tutorialSteps[currentStep].SetActive(false);
            EndTutorial();
        }
    }
    
    public void EndTutorial()
    {
        isTutorialRunning = false;
        Time.timeScale = 1;
        hasStarted = true;
    }
    
    private IEnumerator StartTutorialWithDelay()
    {
        yield return new WaitForSeconds(cutsceneDelay);
        ShowTutorialStep(0);
    }
}
