using System.Collections.Generic;
using Characters.SO.CharacterDataSO.EnemyStateDataSO;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.CharacterDataSO
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "GameData/CharacterData/EnemyData")]
    public class EnemyDataSo : BaseCharacterDataSo
    {
        [FoldoutGroup("Combat")] [SerializeField, PropertyTooltip("Exp drop after dead")]
        private int expDrop;
        
        [FoldoutGroup("Combat")] [SerializeField, PropertyTooltip("Score drop after dead")]
        private int scoreDrop;
        
        [FoldoutGroup("Skills")]
        [SerializeField, PropertyTooltip("Delay before performing skill after being eligible.")]
        private float delayBeforePerformSkill = 0.5f;

        [FoldoutGroup("State Machine Controller")] [SerializeField] [Required]
        private BaseEnemyStateDataSo defaultState;

        [FoldoutGroup("State Machine Controller")]
        [ValidateInput(nameof(ValidateStateList),
            "hpPercentageToEnter ต้องอยู่ที่ 0–100 และต้องเรียงแบบมากไปน้อย (ห้ามเท่ากัน)")]
        [SerializeField] [PropertySpace(10)]
        private List<EnemyStateDataPayload> stateList;

        public int ExpDrop => expDrop;
        public int ScoreDrop => scoreDrop;
        public float DelayBeforePerformSkill => delayBeforePerformSkill;
        public BaseEnemyStateDataSo DefaultState => defaultState;
        public List<EnemyStateDataPayload> StateList => stateList;

        // ===== Odin Validator สำหรับทั้งลิสต์ =====
        private bool ValidateStateList(List<EnemyStateDataPayload> list)
        {
            if (list == null) return true;

            for (int i = 0; i < list.Count; i++)
            {
                float v = list[i].HpPercentageToEnter;

                // ชัวร์ๆ เผื่อมีการกรอกด้วยคีย์บอร์ด
                if (v < 0f || v > 100f)
                    return false;

                // ต้อง "ลดลงอย่างเคร่งครัด" (strictly descending): ก่อนหน้า > ถัดไป และห้ามเท่ากัน
                if (i < list.Count - 1)
                {
                    float next = list[i + 1].HpPercentageToEnter;
                    if (!(v > next)) // ถ้า v <= next ถือว่าผิด
                        return false;
                }
            }

            return true;
        }
    }
}