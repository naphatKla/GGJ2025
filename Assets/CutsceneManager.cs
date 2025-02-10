using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutsceneManager : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void FreezeTheGame()
    {
        Time.timeScale = 0;
    }
    
    public void ResumeTheGame()
    {
        Time.timeScale = 1;
    }

    public void TimelineSetActiveFalse()
    {
        gameObject.SetActive(false);
        
    }
    
    public void TimelineSetActiveTrue()
    {
        gameObject.SetActive(true);
        
    }
    
}
