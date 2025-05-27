using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.UI.Extensions.Examples.FancyScrollViewExample02;

namespace UnityEngine.UI.Extensions
{
    class MapSelectionUI : MonoBehaviour
    {
        [SerializeField] private string sceneLoad;
        [SerializeField] private ScrollView scrollView = default;
        [SerializeField] private Button prevCellButton = default;
        [SerializeField] private Button nextCellButton = default;
        [SerializeField] private Text selectedMapName = default;
        [SerializeField] private Text selectedMapInfo = default;

        [Header("Map Data")]
        [SerializeField] private List<MapDataSO> mapDataList;

        private MapDataSO[] _mapDataArray;

        void Start()
        {
            prevCellButton.onClick.AddListener(scrollView.SelectPrevCell);
            nextCellButton.onClick.AddListener(scrollView.SelectNextCell);
            scrollView.OnSelectionChanged(OnSelectionChanged);

            _mapDataArray = mapDataList.ToArray();
            
            var items = _mapDataArray
                .Select(m => new ItemData(m.mapName, m.Image))
                .ToArray();

            scrollView.UpdateData(items);
            scrollView.SelectCell(0);
        }

        void OnSelectionChanged(int index)
        {
            if (_mapDataArray != null && index >= 0 && index < _mapDataArray.Length)
            {
                selectedMapName.text = "Map " + _mapDataArray[index].mapName;
                selectedMapInfo.text = "Stage " + "0" + "/" +_mapDataArray[index].stages.Count.ToString();
                
                GameController.Instance.selectedMapIndex = index;
                GameController.Instance.selectedMapData = mapDataList[index];
            }
        }
        
        public async void PlaySelected()
        {
            await SceneManager.LoadSceneAsync(sceneLoad).ToUniTask();
            await UniTask.Yield();
        }
    }
}