using Characters.InputSystems;
using Characters.SO.CharacterDataSO;
using UnityEngine;

namespace Characters.Controllers
{
    public class EnemyController : BaseController
    {
        public bool CountedByMax { get; set; }

        public override void AssignCharacterData(BaseCharacterDataSo data)
        {
            base.AssignCharacterData(data);

            if (InputSystem is not EnemyInputReader enemyInputReader)
            {
                Debug.LogWarning("Enemy Input Was Wrong Type!");
                return;
            }

            enemyInputReader.AssignData(this);
        }

        protected override void SubscribeDependency()
        {
            base.SubscribeDependency();

            if (InputSystem is not EnemyInputReader enemyInputReader)
            {
                Debug.LogWarning("Enemy Input Was Wrong Type!");
                return;
            }

            HealthSystem.OnHealthChange += enemyInputReader.UpdateStateOnHealthChanged;
        }

        protected override void UnSubscribeDependency()
        {
            base.UnSubscribeDependency();

            if (InputSystem is not EnemyInputReader enemyInputReader)
            {
                Debug.LogWarning("Enemy Input Was Wrong Type!");
                return;
            }

            HealthSystem.OnHealthChange -= enemyInputReader.UpdateStateOnHealthChanged;
        }

        public override void CancelBehaviorOnDead()
        {
            base.CancelBehaviorOnDead();

            if (InputSystem is not EnemyInputReader enemyInputReader)
            {
                Debug.LogWarning("Enemy Input Was Wrong Type!");
                return;
            }

            enemyInputReader.ResetInputSystem();
        }
    }
}