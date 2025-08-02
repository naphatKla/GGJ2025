using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI.Extensions.Examples.FancyScrollViewExample03;

namespace UI.MapSelection
{
    class MapSelectionPresenter : MonoBehaviour
    {
        [SerializeField] MapSelectionScrollView mapSelectionScrollView;
        [SerializeField] public MapSelectionDataContainer mapSelectionDataContainer;
        

        void Start()
        {
            var items = new List<MapSelectionItemModel>();
            foreach (var mapData in mapSelectionDataContainer.mapSelectionList)
            {
                items.Add(new MapSelectionItemModel($"{mapData.mapName}", mapData.image));
            }
            
            mapSelectionScrollView.UpdateData(items);
            mapSelectionScrollView.SelectCell(0);
        }
    }
}
