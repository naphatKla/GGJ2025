using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneInvoker : MonoBehaviour
{
    public void FadeInFinished()
    {
        GlobalSceneEvents.OnFadeInFinished?.Invoke();
    }

    public void FadeOutFinished()
    {
        GlobalSceneEvents.OnFadeOutFinished?.Invoke();
    }

    public void SceneReady()
    {
        GlobalSceneEvents.OnSceneReady?.Invoke();
    }
}
