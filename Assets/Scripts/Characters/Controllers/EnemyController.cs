using Characters.ComboSystems;
using UnityEngine;

namespace Characters.Controllers
{
    public class EnemyController : BaseController
    {
        void Awake()
        {
            HealthSystem.OnTakeDamage += OnTakeDamage;
        }
        private void OnTakeDamage(BaseController attacker)
        {
            if (attacker.TryGetComponent(out PlayerController playerController))
                playerController.comboSystem.RegisterHit(this);
        }
    }
}
