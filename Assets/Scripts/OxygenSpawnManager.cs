using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

[System.Serializable]
public class OxygenData
{
    public Oxygen oxygenPrefab;
    public float spawnChance;
}

public class OxygenSpawnManager : MMSingleton<OxygenSpawnManager>
{
    [SerializeField] private List<OxygenData> oxygenDataList = new List<OxygenData>();
    [SerializeField] private Transform oxygenParent;
    [SerializeField] private int maxSpawn;
    [SerializeField] private int spawnPerTick;
    [SerializeField] private float timeTick;
    [SerializeField] private Vector2 spawnSize = Vector2.zero;
    private List<Oxygen> expList = new List<Oxygen>();
    public List<Oxygen> AllOfOxygenType => expList;


    private void Start()
    {
        foreach (OxygenData data in oxygenDataList)
        {
            expList.Add(data.oxygenPrefab);
        }
        expList.Sort((x, y) => x.expAmount.CompareTo(y.expAmount));
        expList.Reverse();
        StartCoroutine(RandomExpSpawn());
    }

    private IEnumerator RandomExpSpawn()
    {
        while (true)
        {
            foreach (var exp in oxygenDataList)
            {
                if (!(Random.value <= exp.spawnChance)) continue;
                float elapsedTime = 0f;

                while (elapsedTime < timeTick)
                {
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                if (oxygenParent.childCount >= maxSpawn) continue;
                for (int i = 0; i < spawnPerTick; i++)
                {
                    GameObject obj = Instantiate(exp.oxygenPrefab.gameObject, GetRegionPosition(), Quaternion.identity);
                    obj.transform.parent = oxygenParent;
                }
            }
        }
    }

    private Vector3 GetRegionPosition()
    {
        return new Vector3(
            Random.Range(-spawnSize.x / 2, spawnSize.x / 2),
            Random.Range(-spawnSize.y / 2, spawnSize.y / 2),
            0
        );
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(spawnSize.x, spawnSize.y, 0));
    }
}