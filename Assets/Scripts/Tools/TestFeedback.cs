using System.Collections;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Tools
{
    public class TestFeedback : MonoBehaviour
    {
        [SerializeField] private MMF_Player feedbackTest;
        [SerializeField] private int testRound = 100;
        [SerializeField] private float dtPerRound = 0.01f;

        [Button]
        private void StartTest()
        {
            StartCoroutine(Test());
        }
        
        private IEnumerator Test()
        {
            for (int i = 0; i < testRound; i++)
            {
                feedbackTest.PlayFeedbacks();
                yield return new WaitForSeconds(dtPerRound);
            }
        }
    }
}
