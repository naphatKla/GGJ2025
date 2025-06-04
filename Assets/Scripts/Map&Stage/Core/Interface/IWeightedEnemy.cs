using System.Collections;
using System.Collections.Generic;
using Characters.Controllers;
using Characters.SO.CharacterDataSO;
using UnityEngine;

public interface IWeightedEnemy
{
    EnemyController GetCharacterData();
    float GetSpawnChance();
}
