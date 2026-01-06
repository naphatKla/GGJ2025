using System;
using System.Collections;
using System.Collections.Generic;
using Challenge;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI.Milestone
{
    [Serializable]
    public class MapMilestoneCatagory
    {
        [FoldoutGroup("$mapID")]
        public string mapID;
        [FoldoutGroup("$mapID")]
        public List<ChallengeDataSO> milestoneEntries;
    }

    [CreateAssetMenu(fileName = "MilestoneContainer", menuName = "MilestoneUIContainer")]
    public class MilestoneDataContainer : ScriptableObject
    {
        public List<MapMilestoneCatagory> milestoneList;
    }
}
