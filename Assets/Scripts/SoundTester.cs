using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;

public class SoundTester : MonoBehaviour
{
    [SerializeField] private MMF_Player sound;

    public void PlaySound()
    {
        sound.PlayFeedbacks();
    }
}
