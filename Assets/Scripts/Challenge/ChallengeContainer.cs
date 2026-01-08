using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UI.MapSelection;
using UnityEngine;

namespace Challenge
{
    [CreateAssetMenu(fileName = "ChallengeContainer", menuName = "Challenge/ChallengeContainer")]
    public class ChallengeContainer : ScriptableObject
    {
        [Title("Challenge List")]
        public List<ChallengeDataSO> challengeList;
    }
}