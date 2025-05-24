using System;
using System.Collections;
using System.Collections.Generic;
using Characters.Controllers;
using Characters.SO.CharacterDataSO;
using UnityEngine;

public interface IEnemyData
{
    EnemyController EnemyController { get; }
}

[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/EnemyData", order = 1)]
public class EnemyDataSO : ScriptableObject, IEnemyData
{
    [SerializeField] private EnemyController genericEnemyPrefab;
    [SerializeField] public List<CharacterDataSo> enemyData;
    
    public EnemyController EnemyController => genericEnemyPrefab;
}
