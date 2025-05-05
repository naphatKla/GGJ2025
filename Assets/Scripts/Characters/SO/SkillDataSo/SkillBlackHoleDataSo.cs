using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    [CreateAssetMenu(fileName = "SkillBlackHoleData", menuName = "GameData/SkillData/SkillBlackHoleData")]
    public class SkillBlackHoleDataSo : BaseSkillDataSo
    {
        [SerializeField] private int cloneAmount;
        [SerializeField] private float cloneDamage;

        public int CloneAmount => cloneAmount;
        public float CloneDamage => cloneDamage;
    }
}
