/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

using UnityEngine;

namespace UI.MapSelection
{
    class MapSelectionItemModel
    {
        public string MapName { get; }
        public Sprite MapImage { get; }

        public MapSelectionItemModel(string message, Sprite image)
        {
            MapName = message;
            MapImage = image;
        }
    }
}
