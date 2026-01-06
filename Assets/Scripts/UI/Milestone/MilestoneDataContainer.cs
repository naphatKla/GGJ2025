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
        [FoldoutGroup("$catagoryName")]
        public string catagoryName;
        [FoldoutGroup("$catagoryName")]
        public List<MilestoneEntry> storageEntries;
    }
    
    [Serializable]
    public struct MilestoneEntry
    {
        public int level;
        public ChallengeDataSO challenge;
    }
    
    [CreateAssetMenu(fileName = "MilestoneContainer", menuName = "Map Selection")]
    public class MilestoneDataContainer : ScriptableObject
    {
        public List<MapMilestoneCatagory> milestoneList;
    }
}
