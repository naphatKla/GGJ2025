using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[System.Serializable]
public class ExpData
{
    public GameObject expPrefab;
    public int expAmount;
    public float expChance;
}

public class RandomSpawnExp : MonoBehaviour
{
    [SerializeField] private List<ExpData> expDataList = new List<ExpData>();
    [SerializeField] public Transform expParent;
    [SerializeField] private int maxSpawn;
    [SerializeField] private float expSpawnTimer;
    [Tooltip("The size of spawn region")]
    [BoxGroup("Size")] public Vector2 regionSize = Vector2.zero;


    private void Start()
    {
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
                        GameObject obj = Instantiate(exp.expPrefab, GetRegionPosition(), Quaternion.identity);
                        obj.transform.parent = expParent;
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