using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameControl.Controller;
using GameControl.GameState;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum UIPanelType
{
    None,
    Pause,
    MapResult,
    SkillTree,
    SolfUpgrade,
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

public class UIManager : MMSingleton<UIManager>
{
    [SerializeField] private string menuScene;
    [SerializeField] private string gamePlayScene;

    [Header("UI Panels")] public List<UIPanelEntry> panelEntries;

    private readonly Dictionary<UIPanelType, GameObject> panelDict = new();
    private readonly Stack<UIPanelType> panelStack = new();
    private bool _isPaused = false;
    public event Action OnAnyPanelOpen;
    public event Action OnAllPanelClosed;

    protected override void Awake()
    {
        base.Awake();

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
        OnAnyPanelOpen?.Invoke();
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
            return;
        }
        
        OnAllPanelClosed?.Invoke();
    }
    
    public void CloseSpecificPanel(UIPanelType type)
    {
        if (!panelDict.ContainsKey(type)) return;
        if (!panelStack.Contains(type)) return;
        
        if (panelStack.Peek() == type)
        {
            ClosePanel();
            return;
        }

        var tempStack = new Stack<UIPanelType>();
        while (panelStack.Count > 0)
        {
            var current = panelStack.Pop();
            if (current == type)
            {
                panelDict[current].SetActive(false);
                break;
            }
            tempStack.Push(current);
        }
        while (tempStack.Count > 0) panelStack.Push(tempStack.Pop());
    }


    public void CloseAllPanels()
    {
        while (panelStack.Count > 0)
        {
            var top = panelStack.Pop();
            panelDict[top].SetActive(false);
        }
            
        OnAllPanelClosed?.Invoke();
        MMTimeScaleEvent.Trigger(MMTimeScaleMethods.Reset, 1, -1, false, 0f, false);
        Time.timeScale = 1;
    }
    
    public async void BackMenu()
    {
        await SceneManager.LoadSceneAsync(menuScene).ToUniTask();
        await UniTask.Yield();
    }

    public void OpenPausePanel()
    {
        if (!_isPaused)
        {
            MMTimeScaleEvent.Trigger(MMTimeScaleMethods.For, 0, -1, true, 10f, true);
            OpenPanel(UIPanelType.Pause);
            _isPaused = true;
        }
        else
        {
            if (GameStateController.Instance.CurrentState is not SummaryState) 
                MMTimeScaleEvent.Trigger(MMTimeScaleMethods.Reset, 1, -1, false, 0f, false);
            CloseSpecificPanel(UIPanelType.Pause);
            _isPaused = false;
        }
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
    
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game");
    }
}