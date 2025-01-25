using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

[System.Serializable]
public class ExpData
{
    public ExpScript expPrefab;
    public float expChance;
}

public class RandomSpawnExp : MMSingleton<RandomSpawnExp>
{
    [SerializeField] private List<ExpData> expDataList = new List<ExpData>();
    [SerializeField] public Transform expParent;
    [SerializeField] private int maxSpawn;
    [SerializeField] private int expPerSpawn;
    [SerializeField] private float expSpawnTimer;
    [Tooltip("The size of spawn region")]
    [BoxGroup("Size")] public Vector2 regionSize = Vector2.zero;
    private List<ExpScript> expList = new List<ExpScript>();
    public List<ExpScript> OxygenAvailable => expList;


    private void Start()
    {
        foreach (ExpData data in expDataList)
        {
            expList.Add(data.expPrefab);
        }
        expList.Sort((x, y) => x.expAmount.CompareTo(y.expAmount));
        expList.Reverse();
        StartCoroutine(RandomExpSpawn());
    }

    private IEnumerator RandomExpSpawn()
    {
        while (true)
        {
            foreach (var exp in expDataList)
            {
                if (Random.value <= exp.expChance)
                {
                    float elapsedTime = 0f;

                    while (elapsedTime < expSpawnTimer)
                    {
                        elapsedTime += Time.deltaTime;
                        yield return null;
                    }
                
                    if ( expParent.childCount < maxSpawn)
                    {
                        for (int i = 0; i < expPerSpawn; i++)
                        {
                            GameObject obj = Instantiate(exp.expPrefab.gameObject, GetRegionPosition(), Quaternion.identity);
                            obj.transform.parent = expParent;
                        }
                    }
                }
            }
        }
    }

    private Vector3 GetRegionPosition()
    {
        return new Vector3(
            Random.Range(-regionSize.x / 2, regionSize.x / 2),
            Random.Range(-regionSize.y / 2, regionSize.y / 2),
            0
        );
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(regionSize.x, regionSize.y, 0));
    }
}