using Characters.InputSystems;
using Characters.SO.CharacterDataSO;
using UnityEngine;

namespace Characters.Controllers
{
    public class EnemyController : BaseController
    {
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
    }
}
