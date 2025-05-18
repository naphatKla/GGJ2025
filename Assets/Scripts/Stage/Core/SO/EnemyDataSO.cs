using System.Collections;
using System.Collections.Generic;
using Characters.Controllers;
using UnityEngine;

public interface IEnemyData
{
    EnemyController EnemyController { get; }
}

[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/EnemyData", order = 1)]
public class EnemyDataSO : ScriptableObject, IEnemyData
{
    [SerializeField] private EnemyController enemyPrefab;
    public EnemyController EnemyController => enemyPrefab;
}
