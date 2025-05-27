using UnityEngine;

namespace UnityEngine.UI.Extensions.Examples.FancyScrollViewExample02
{
    class ItemData
    {
        public string Message { get; }
        public Sprite Image { get; }

        public ItemData(string message, Sprite image)
        {
            Message = message;
            Image = image;
        }
    }
}