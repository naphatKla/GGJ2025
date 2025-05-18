using UnityEngine;

namespace Characters.Controllers
{
    public class EnemyController : BaseController
    {
        [SerializeField] private GameObject prefab;
        public GameObject Prefab => prefab;
    }
}
