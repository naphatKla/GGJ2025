using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using ProjectExtensions;
using UnityEngine;

namespace UI.MapSelection
{
    public class MapSelectionSender : MMSingleton<MapSelectionSender>
    {
        public int currentMapSelectionIndex = 0;
        public MapSelectionDataContainer currentmapSelectionDataContainer;
        
        void Start()
        {
            DontDestroyOnLoad(this);
        }
    }
}