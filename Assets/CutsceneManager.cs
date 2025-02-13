using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;

public class CutsceneManager : MonoBehaviour
{
    public PlayableDirector playableDirector;
    public PlayableDirector fastIntro;
    private static bool _isPlayOnce;

    private void Awake()
    {
        if (_isPlayOnce)
        {
            fastIntro.Play();
            return;
        }
        
        playableDirector.Play();
        _isPlayOnce = true;
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
