using System.Collections;
using System.Collections.Generic;
using GameControl.Controller;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameControl.SO
{
    [CreateAssetMenu(menuName = "SpawnConditions/Time Window")]
    public class TimeWindowSpawnCondition : SpawnConditionSO
    {
        [Tooltip("เวลาตั้งแต่เริ่มเกม (วินาที)")]
        [InfoBox("เช่นอยากให้เกิดหลังจากเกมเริ่ม 30 วิก็ใส่ไป 30 วิ")]
        public float startAfter = 0f;
        [Tooltip("เวลาที่สิ้นสุด (วินาที) (ถ้า < 0 = ไม่มีขีดจำกัด)")]
        [InfoBox("ถ้าอยากให้มันไม่เกิดหลังจาก 120 วิก็ใส่ไป 120 วิ")]
        public float endAt = -1f;

        public override bool IsSatisfied(SpawnerStateController state, MapDataSO mapData, MapDataSO.EnemyOption option)
        {
            var elapsed = GameTimer.Instance.GlobalTimerDown;
            if (elapsed < startAfter) return false;
            if (endAt >= 0f && elapsed > endAt) return false;
            return true;
        }
    }

}
