using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEnemyData
{
    GameObject EnemyPrefab { get; }
}

[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/EnemyData", order = 1)]
public class EnemyDataSO : ScriptableObject, IEnemyData
{
    
    [SerializeField] private GameObject enemyPrefab;
    public GameObject EnemyPrefab => enemyPrefab;
}
