using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;

namespace UI.MapSelection
{
    public class MapSelectionSender : MMSingleton<MapSelectionSender>
    {
        public int currentMapSelectionIndex = 0;
        
        void Start()
        {
            DontDestroyOnLoad(this);
        }
    }
}