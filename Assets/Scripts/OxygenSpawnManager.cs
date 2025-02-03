using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;
using Random = UnityEngine.Random;

[System.Serializable]
public class OxygenData
{
    public Oxygen oxygenPrefab;
    public float spawnChance;
}

public class OxygenSpawnManager : MMSingleton<OxygenSpawnManager>
{
    #region Inspectors & Fields 
    [SerializeField] private List<OxygenData> oxygenDataList = new List<OxygenData>();
    [SerializeField] private Transform oxygenParent;
    [SerializeField] private int maxSpawn;
    [SerializeField] private int spawnPerTick;
    [SerializeField] private float timeTick;
    [SerializeField] private Vector2 spawnSize = Vector2.zero;
    private readonly List<Oxygen> _oxygenList = new List<Oxygen>();
    #endregion -----------------------------------------------------------------------------------------------------

    #region Properties 
    public List<Oxygen> AllOfOxygenType => _oxygenList;
    #endregion -----------------------------------------------------------------------------------------------------
    
    #region UnityMethods 
    private void Start()
    {
        foreach (OxygenData data in oxygenDataList)
            _oxygenList.Add(data.oxygenPrefab);
        _oxygenList.Sort((x, y) => x.expAmount.CompareTo(y.expAmount));
        _oxygenList.Reverse();
        StartCoroutine(RandomOxygenSpawn());
    }
    
    private IEnumerator RandomOxygenSpawn()
    {
        while (true)
        {
            yield return new WaitUntil(() => oxygenParent.childCount < maxSpawn);
            foreach (OxygenData oxygen in oxygenDataList)
            {
                if (Random.value > oxygen.spawnChance) continue;
                yield return new WaitForSeconds(timeTick);
                if (oxygenParent.childCount >= maxSpawn) break;
                int spawnCount = Mathf.Clamp(maxSpawn - oxygenParent.childCount, 0, spawnPerTick);
                for (int i = 0; i < spawnCount; i++) 
                    Instantiate(oxygen.oxygenPrefab.gameObject, GetRegionPosition(), Quaternion.identity,oxygenParent);
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(spawnSize.x, spawnSize.y, 0));
    }
    #endregion -----------------------------------------------------------------------------------------------------

    #region Methods
    private Vector3 GetRegionPosition()
    {
        return new Vector3(
            Random.Range(-spawnSize.x / 2, spawnSize.x / 2),
            Random.Range(-spawnSize.y / 2, spawnSize.y / 2),
            0
        );
    }
    #endregion-----------------------------------------------------------------------------------------------------
}