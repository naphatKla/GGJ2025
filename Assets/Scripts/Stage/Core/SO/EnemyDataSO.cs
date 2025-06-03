using System;
using System.Collections;
using System.Collections.Generic;
using Characters.Controllers;
using Characters.SO.CharacterDataSO;
using UnityEngine;

public interface IEnemyData
{
    List<EnemyController> EnemyController { get; }
}

[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/EnemyData", order = 1)]
public class EnemyDataSO : ScriptableObject, IEnemyData
{
    [SerializeField] public List<EnemyController> enemyData;
    
    public List<EnemyController> EnemyController => enemyData;
}
