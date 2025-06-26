using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;

public class MenuVideoLooper : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public List<VideoClip> videoClips;
    public bool playRandom = false;

    private int currentIndex = 0;
    private List<int> playedIndices = new List<int>();

    void Start()
    {
        if (videoPlayer == null || videoClips.Count == 0)
        {
            Debug.LogError("VideoPlayer or videoClips not assigned");
            return;
        }

        videoPlayer.isLooping = false;
        videoPlayer.loopPointReached += OnVideoEnd;

        if (playRandom && videoClips.Count > 1)
        {
            PlayRandomVideo();
        }
        else
        {
            videoPlayer.clip = videoClips[0];
            videoPlayer.Play();
            videoPlayer.isLooping = true;
        }
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        if (!playRandom) return;
        
        if (playedIndices.Count < videoClips.Count)
        {
            PlayRandomVideo();
        }
        else
        {
            playedIndices.Clear();
            PlayRandomVideo();
        }
    }

    void PlayRandomVideo()
    {
        int nextIndex;
        do
        {
            nextIndex = Random.Range(0, videoClips.Count);
        } while (playedIndices.Contains(nextIndex));

        playedIndices.Add(nextIndex);
        currentIndex = nextIndex;

        videoPlayer.clip = videoClips[currentIndex];
        videoPlayer.Play();
    }
}
