using System;
using UnityEngine;

[Serializable]
public class AudioEntry
{
    public AudioClip clip;
    public AudioSource source;
    [Range(0f, 1f)] public float volume = 1f;
    public bool loop = false;
}

public class AudioManager : MonoBehaviour
{
    #region Inspector Variables
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AudioManager>();

                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject();
                    _instance = singletonObject.AddComponent<AudioManager>();
                    singletonObject.name = typeof(AudioManager).ToString() + " (Singleton)";
                    DontDestroyOnLoad(singletonObject);
                }
            }
            return _instance;
        }
    }

    [SerializeField] public SerializableDictionary<string, AudioEntry> audioClipDictionary;
    #endregion

    #region Method
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Play audio by key
    /// </summary>
    /// <param name="key (the name of the Audio in AudioManager)"></param>
    public void PlayAudio(string key)
    {
        if (audioClipDictionary.ContainsKey(key))
        {
            AudioEntry entry = audioClipDictionary[key];
            if (entry.clip != null && entry.source != null)
            {
                entry.source.clip = entry.clip;
                entry.source.volume = entry.volume;
                entry.source.loop = entry.loop;
                entry.source.Play();
            }
            else
            {
                Debug.LogWarning($"AudioClip or AudioSource for key '{key}' is null!");
            }
        }
        else
        {
            Debug.LogWarning($"Audio '{key}' not found in audio dictionary!");
        }
    }

    /// <summary>
    /// Stop audio by key
    /// </summary>
    /// <param name="key (the name of the Audio in AudioManager)"></param>
    public void StopAudio(string key)
    {
        if (audioClipDictionary.ContainsKey(key))
        {
            AudioEntry entry = audioClipDictionary[key];
            if (entry.clip != null && entry.source != null)
            {
                entry.source.Stop();
            }
            else
            {
                Debug.LogWarning($"AudioClip or AudioSource for key '{key}' is null!");
            }
        }
        else
        {
            Debug.LogWarning($"Audio '{key}' not found in audio dictionary!");
        }
    }

    /// <summary>
    /// Pause audio by key
    /// </summary>
    /// <param name="key (the name of the Audio in AudioManager)"></param>
    public void PauseAudio(string key)
    {
        if (audioClipDictionary.ContainsKey(key))
        {
            AudioEntry entry = audioClipDictionary[key];
            if (entry.clip != null && entry.source != null)
            {
                entry.source.Pause();
            }
            else
            {
                Debug.LogWarning($"AudioClip or AudioSource for key '{key}' is null!");
            }
        }
        else
        {
            Debug.LogWarning($"Audio '{key}' not found in audio dictionary!");
        }
    }

    /// <summary>
    /// Stop all audio
    /// </summary>
    public void StopAllAudio()
    {
        foreach (var entry in audioClipDictionary)
        {
            if (entry.Value.source.isPlaying)
            {
                entry.Value.source.Stop();
            }
        }
    }
    #endregion
}