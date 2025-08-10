using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    [CreateAssetMenu(fileName = "SkillOverloopData", menuName = "GameData/SkillData/SkillOverloopData")]
    public class SkillOverloopDataSo : BaseSkillDataSo
    {
        [SerializeField] private int amountSkillToReset = 1;
        [SerializeField] private int resetRound = 1;
        [SerializeField] private float delayPerRound = 1f;
    }
}
