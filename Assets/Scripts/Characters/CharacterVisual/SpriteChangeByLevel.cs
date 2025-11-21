using System;
using System.Collections.Generic;
using Characters.LevelSystems;
using UnityEngine;

[Serializable]
public struct SpriteLevelChangerData
{
    public GameObject sprite;
    public int LevelThreshold;
}
    
public class SpriteChangeByLevel : MonoBehaviour
{
    [SerializeField] private LevelSystem levelSystem;
    [SerializeField] private List<SpriteLevelChangerData> spriteLevelChangerDatas;

    private void OnEnable()
    {
        levelSystem.OnLevelUp += ChangeSprite;
    }

    private void OnDisable()
    {
        levelSystem.OnLevelUp -= ChangeSprite;
    }

    private void ChangeSprite(int level)
    {
        foreach (var data in spriteLevelChangerDatas)
        {
            if (level < data.LevelThreshold) continue;
            data.sprite.SetActive(true);
        }
    }
}
