using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPickup : MonoBehaviour
{
    [SerializeField]
    private Vector2 detectDistance;
    void Update()
    {
        
    }

    public void FindExpOrb()
    {
        
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector2.zero, new Vector2(detectDistance.x, detectDistance.y));
    }
}
