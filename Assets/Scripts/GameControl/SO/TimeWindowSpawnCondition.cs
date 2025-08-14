using System.Collections;
using System.Collections.Generic;
using GameControl.Controller;
using UnityEngine;

namespace GameControl.SO
{
    [CreateAssetMenu(menuName = "SpawnConditions/Time Window")]
    public class TimeWindowSpawnCondition : SpawnConditionSO
    {
        [Tooltip("เวลาตั้งแต่เริ่มเกม (วินาที)")]
        public float startAfter = 0f;
        [Tooltip("เวลาที่สิ้นสุด (วินาที) (ถ้า < 0 = ไม่มีขีดจำกัด)")]
        public float endAt = -1f;

        public override bool IsSatisfied(SpawnerStateController state, EnemySpawnerController spawner, MapDataSO mapData, MapDataSO.EnemyOption option)
        {
            var elapsed = GameTimer.Instance.GlobalTimerDown;
            if (elapsed < startAfter) return false;
            if (endAt >= 0f && elapsed > endAt) return false;
            return true;
        }
    }

}
