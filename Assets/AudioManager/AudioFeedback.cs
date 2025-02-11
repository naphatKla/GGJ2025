using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

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
    [Tooltip("Reference to the AudioMixer")]
    public AudioMixer audioMixer;
    [Tooltip("Category of the audio clips")]
    [SerializeField] public SerializableDictionary<string, AudioEntryFeedback> soundFeedbacks;
    
    [Tooltip("GameObject AudioSource")]
    public GameObject poolObject;

    private Queue<AudioSource> audioPool = new Queue<AudioSource>();
    private List<AudioSource> activeSources = new List<AudioSource>();

    private void Start()
    {
        if (poolObject != null)
        {
            var sources = poolObject.GetComponentsInChildren<AudioSource>();
            foreach (var source in sources)
            {
                source.Stop();
                source.clip = null;
                audioPool.Enqueue(source);
            }
        }
    }

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

    private AudioSource GetAudioSource()
    {
        foreach (var source in audioPool)
            if (!source.isPlaying)
            {
                source.gameObject.SetActive(true);
                source.Stop();
                source.clip = null;
                audioPool.Dequeue();
                return source;
            }

        return null;
    }

    private void ReturnAudioSource(AudioSource source)
    {
        StartCoroutine(WaitForAudioToFinish(source));
    }

    private IEnumerator WaitForAudioToFinish(AudioSource source)
    {
        yield return new WaitUntil(() => !source.isPlaying);

        source.Stop();
        source.clip = null;

        if (activeSources.Count == 1) poolObject.SetActive(false);

        activeSources.Remove(source);
        audioPool.Enqueue(source);
    }

    public void PlayMultipleAudio(string category, string outputGroupName = null)
    {
        if (soundFeedbacks.TryGetValue(category, out var audioEntry))
        {
            if (audioEntry.clip == null || audioEntry.clip.Length == 0)
            {
                Debug.LogWarning($"No audio clips assigned for key: {category}");
                return;
            }

            AudioMixerGroup outputGroup = null;
            if (!string.IsNullOrEmpty(outputGroupName) && audioMixer != null)
            {
                var groups = audioMixer.FindMatchingGroups(outputGroupName);
                if (groups.Length > 0)
                    outputGroup = groups[0];
                else
                    Debug.LogWarning($"AudioMixerGroup '{outputGroupName}' not found!");
            }

            poolObject.SetActive(true);

            foreach (var clip in audioEntry.clip)
            {
                var source = GetAudioSource();
                if (source == null) continue;

                source.clip = clip;
                source.volume = audioEntry.volume;
                source.loop = audioEntry.loop;
                if (outputGroup != null) source.outputAudioMixerGroup = outputGroup;

                source.Play();
                activeSources.Add(source);

                StartCoroutine(ReleaseAfterPlay(source, clip.length));
            }
        }
        else
        {
            Debug.LogWarning($"Key not found in soundFeedbacks: {category}");
        }
    }

    private IEnumerator ReleaseAfterPlay(AudioSource source, float delay)
    {
        yield return new WaitUntil(() => !source.isPlaying);
        ReturnAudioSource(source);
    }
}
