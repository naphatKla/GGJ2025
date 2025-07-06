using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameControl
{
    public class ColliderActivaterZone : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Enemy"))
            {
                Collider2D col = other.gameObject.GetComponent<Collider2D>();
                if (col != null && col.isTrigger)
                    col.isTrigger = false;
            }
        }
    }
}
