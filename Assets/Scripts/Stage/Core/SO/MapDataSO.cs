using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapData", menuName = "Game/Map Data", order = 0)]
public class MapDataSO : ScriptableObject
{
    [Tooltip("Name of this map")]
    public string mapName;

    [Tooltip("Background image to use for this map")]
    public Sprite background;
    
    [Tooltip("Map Image")]
    public Sprite Image;

    [Tooltip("List of stages under this map")]
    public List<StageDataSO> stages;
}