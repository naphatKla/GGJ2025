using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Challenge
{
    [CreateAssetMenu(fileName = "ChallengeContainer", menuName = "Challenge/ChallengeContainer")]
    public class ChallengeContainer : ScriptableObject
    {
        public List<ChallengeDataSO> challengeList;
    }
}