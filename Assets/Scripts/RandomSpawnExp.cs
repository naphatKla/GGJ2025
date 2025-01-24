using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[System.Serializable]
public class ExpData
{
    public GameObject expPrefab;
    public int expAmount;
    public int maxSpawn;
    public float expSpawnTimer;
}

public class RandomSpawnExp : MonoBehaviour
{
    [SerializeField] private List<ExpData> expDataList = new List<ExpData>();
    [SerializeField] private Transform expParent;
    [Tooltip("The size of spawn region")]
    [BoxGroup("Size")] public Vector2 regionSize = Vector2.zero;
    
    private Dictionary<GameObject, int> expCountPerType = new Dictionary<GameObject, int>();

    private void Start()
    {
        StartCoroutine(RandomExpSpawn());
        
        foreach (var exp in expDataList)
        {
            expCountPerType[exp.expPrefab] = 0;
        }
        
    }

    private IEnumerator RandomExpSpawn()
    {
        while (true)
        {
            foreach (var exp in expDataList)
            {
                float elapsedTime = 0f;

                while (elapsedTime < exp.expSpawnTimer)
                {
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
                
                if ( expCountPerType[exp.expPrefab] < exp.maxSpawn)
                {
                    GameObject obj = Instantiate(exp.expPrefab, GetRegionPosition(), Quaternion.identity);
                    obj.transform.parent = expParent;
                    expCountPerType[exp.expPrefab]++;
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