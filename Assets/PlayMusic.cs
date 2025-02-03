using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayMusic : MonoBehaviour
{
    private AudioFeedback audioFeedback;
    void Start()
    {
        audioFeedback = GetComponent<AudioFeedback>();
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            audioFeedback.PlayAudio("Hit");
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            audioFeedback.PlaySpecificAudio("Hit", 2);
        }
    }
}
