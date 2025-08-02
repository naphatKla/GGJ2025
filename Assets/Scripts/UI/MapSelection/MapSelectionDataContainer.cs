using System.Collections;
using System.Collections.Generic;
using GameControl.SO;
using UnityEngine;

namespace UI.MapSelection
{
    [CreateAssetMenu(fileName = "MapSelectionContainer", menuName = "Map Selection")]
    public class MapSelectionDataContainer : ScriptableObject
    {
        public List<MapDataSO> mapSelectionList;
    }
}
