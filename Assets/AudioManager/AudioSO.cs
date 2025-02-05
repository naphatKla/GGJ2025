using UnityEngine;

[CreateAssetMenu(fileName = "AudioSO", menuName = "AudioSO", order = 1)]
public class AudioSO : ScriptableObject
{
    public string audioName;
    [Range(0f, 1f)] public float volume = 1f;
}
