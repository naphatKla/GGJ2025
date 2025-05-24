using System.Collections;
using System.Collections.Generic;
using Characters.SO.CharacterDataSO;
using UnityEngine;

public interface IWeightedEnemy
{
    CharacterDataSo GetCharacterData();
    float GetSpawnChance();
}
