using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEnemyData
{
    GameObject EnemyPrefab { get; }
    float SpawnChance { get; }
}

[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/EnemyData", order = 1)]
public class EnemyDataSO : ScriptableObject, IEnemyData
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnChance;

    public GameObject EnemyPrefab => enemyPrefab;
    public float SpawnChance => spawnChance;
}
