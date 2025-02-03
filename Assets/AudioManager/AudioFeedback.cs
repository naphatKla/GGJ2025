using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AudioEntryFeedback
{
    [Tooltip("Insert the audio clips here")]
    public AudioClip[] clip;
    public AudioSource source;
    [Range(0f, 1f)] public float volume = 1f;
    public bool loop = false;
    public bool randomize = false;
}

public class AudioFeedback : MonoBehaviour
{
    [Tooltip("Category of the audio clips")]
    [SerializeField] public SerializableDictionary<string, AudioEntryFeedback> soundFeedbacks;
    
    /// <summary>
    /// Play audio by category
    /// </summary>
    /// <param name="key (catagory)"></param>
    public void PlayAudio(string category)
    {
        if (soundFeedbacks.TryGetValue(category, out AudioEntryFeedback audioEntry))
        {
            if (audioEntry.clip == null || audioEntry.clip.Length == 0)
            {
                Debug.LogWarning($"No audio clips assigned for key: {category}");
                return;
            }
            
            AudioClip clipToPlay = audioEntry.randomize
                ? audioEntry.clip[UnityEngine.Random.Range(0, audioEntry.clip.Length)]
                : audioEntry.clip[0];
            
            audioEntry.source.clip = clipToPlay;
            audioEntry.source.volume = audioEntry.volume;
            audioEntry.source.loop = audioEntry.loop;
            audioEntry.source.Play();
        }
        else
        {
            Debug.LogWarning($"Key not found in soundFeedbacks: {category}");
        }
    }
    
    /// <summary>
    /// Play a specific audio clip from the list
    /// </summary>
    /// <param name="category">The category (key) of the audio entry.</param>
    /// <param name="sound">The index of the specific audio clip to play.</param>
    public void PlaySpecificAudio(string category, int sound)
    {
        if (soundFeedbacks.TryGetValue(category, out AudioEntryFeedback audioEntry))
        {
            if (audioEntry.clip == null || audioEntry.clip.Length == 0)
            {
                Debug.LogWarning($"No audio clips assigned for key: {category}");
                return;
            }
            
            if (sound < 0 || sound >= audioEntry.clip.Length)
            {
                Debug.LogWarning($"Sound index {sound} is out of bounds for category: {category}. Valid range is 0 to {audioEntry.clip.Length - 1}.");
                return;
            }
            
            AudioClip clipToPlay = audioEntry.clip[sound];
            
            audioEntry.source.clip = clipToPlay;
            audioEntry.source.volume = audioEntry.volume;
            audioEntry.source.loop = audioEntry.loop;
            audioEntry.source.Play();
        }
        else
        {
            Debug.LogWarning($"Key not found in soundFeedbacks: {category}");
        }
    }
}
