using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameControl.Controller;
using GameControl.GameState;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum UIPanelType
{
    None,
    Pause,
    MapResult,
    SkillTree,
    Setting,
    SaveGame,
    GameMode,
    MapSelect,
    QuitPanel,
}

[System.Serializable]
public class UIPanelEntry
{
    public UIPanelType type;
    public GameObject panel;
}

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    [SerializeField] private string menuScene;
    [SerializeField] private string gamePlayScene;

    [Header("UI Panels")] public List<UIPanelEntry> panelEntries;

    private readonly Dictionary<UIPanelType, GameObject> panelDict = new();
    private readonly Stack<UIPanelType> panelStack = new();
    private bool _isPaused = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        foreach (var entry in panelEntries)
            if (!panelDict.ContainsKey(entry.type))
            {
                panelDict.Add(entry.type, entry.panel);
                entry.panel.SetActive(false);
            }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OpenPausePanel();
        }
    }

    public void OpenPanel(UIPanelType type)
    {
        if (!panelDict.ContainsKey(type))
        {
            Debug.LogWarning($"[UIManager] panelDict doesn't contain type: {type}");
            return;
        }
        
        if (panelStack.Count > 0 && panelStack.Peek() == type)
        {
            ClosePanel();
            return;
        }
        
        if (panelStack.Count > 0)
        {
            var current = panelStack.Peek();
            panelDict[current].SetActive(false);
        }
        panelDict[type].SetActive(true);
        panelStack.Push(type);
    }


    public void ClosePanel()
    {
        if (panelStack.Count == 0)
            return;

        var top = panelStack.Pop();
        panelDict[top].SetActive(false);
        
        if (panelStack.Count > 0)
        {
            var previous = panelStack.Peek();
            panelDict[previous].SetActive(true);
        }
    }


    public void CloseAllPanels()
    {
        while (panelStack.Count > 0)
        {
            var top = panelStack.Pop();
            panelDict[top].SetActive(false);
        }

        Time.timeScale = 1;
    }
    
    public async void BackMenu()
    {
        ClearGameController();
        await SceneManager.LoadSceneAsync(menuScene).ToUniTask();
        await UniTask.Yield();
    }
    
    public void ClearGameController()
    {
        /*if (GameController.Instance == null) return;
        GameController.Instance.nextStageIndex = 0;
        GameController.Instance.selectedMapIndex = 0;*/
    }
    
    public void OpenPausePanel()
    {
        if (Time.timeScale > 0 && Time.timeScale < 1) return;
        OpenPanel(UIPanelType.Pause);
        
        if (GameStateController.Instance.CurrentState is SummaryState) 
            Time.timeScale = 0;
        else 
            Time.timeScale = Time.timeScale == 0 ? 1 : 0;
    }

    
    public void OpenSaveGamePanel()
    {
        OpenPanel(UIPanelType.SaveGame);
    }
    
    public void OpenMapSelectPanel()
    {
        OpenPanel(UIPanelType.MapSelect);
    }
    public void OpenGameModePanel()
    {
        OpenPanel(UIPanelType.GameMode);
    }

    public void LoadToGamePlayScene()
    {
        SceneManager.LoadScene(gamePlayScene);
    }
    
    public void OpenQuitPanel()
    {
        OpenPanel(UIPanelType.QuitPanel);
    }
}