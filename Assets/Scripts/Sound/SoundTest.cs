using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using UnityEngine;

public class SoundTest : MonoBehaviour
{
    [FoldoutGroup("Feedback")] [SerializeField] private MMF_Player clickSound;

    public void PlaySound()
    {
        clickSound.PlayFeedbacks();
    }
}
