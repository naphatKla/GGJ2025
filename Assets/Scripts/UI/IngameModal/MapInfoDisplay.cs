using GameControl.Controller;
using GameControl.SO;
using TMPro;
using UnityEngine;

namespace UI
{
    public class MapInfoDisplay : MonoBehaviour
    {
        [SerializeField] private MapDataSO currentMapData;
        [SerializeField] private TMP_Text mapName;

        private void Update()
        {
            if (GameStateController.Instance.CurrentMap == null) return;

            if (currentMapData == GameStateController.Instance.CurrentMap) return;
            mapName.text = "MAP " + GameStateController.Instance.CurrentMap.mapName;
        }
    }

}
