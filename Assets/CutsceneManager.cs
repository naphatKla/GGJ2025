using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;

public class CutsceneManager : MonoBehaviour
{
    public PlayableDirector playableDirector;

    private void Awake()
    {
        playableDirector.Play();
    }

    [Button("Play Cutscene")]
    public void PlayCutscene()
    {
        if (playableDirector.gameObject == null) return;
        playableDirector.Play();
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
