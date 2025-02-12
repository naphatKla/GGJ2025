using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using Unity.VisualScripting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class LevelSelector : MonoBehaviour
{
    private LevelSelectorButton currentSelectedLevel;
    public int level;
    [SerializeField] private MMF_Player changeScene;
    private static LevelSelector instance;
    public int[] levelScores = new int[6]{0,0,0,0,0,0};

private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); // ถ้ามี instance อยู่แล้ว ให้ทำลายตัวใหม่
        }
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void SetLevel(LevelSelectorButton button)
    {
        if (currentSelectedLevel == button) return;
        if (currentSelectedLevel != null )
        {
            currentSelectedLevel.TurnNormal();
        }
        currentSelectedLevel = button;
        currentSelectedLevel.OnToggle();
        level = int.Parse(currentSelectedLevel.gameObject.name);
    }

    public void SetScoreOnLevel(int level,int score)
    {
        levelScores[level] = score;
    }
}
