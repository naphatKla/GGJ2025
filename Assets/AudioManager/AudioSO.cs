using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioSO", menuName = "AudioSO", order = 1)]
public class AudioSO : ScriptableObject
{
    public string name;
    [Range(0f, 1f)] public float volume = 1f;
}
