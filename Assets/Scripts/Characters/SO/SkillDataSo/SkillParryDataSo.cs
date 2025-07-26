using UnityEngine;
namespace Characters.SO.SkillDataSo
{
    
    [CreateAssetMenu(fileName = "SkillParryData", menuName = "GameData/SkillData/SkillParryData")]
    public class SkillParryDataSo : BaseSkillDataSo
    {
        [SerializeField] private float parryDuration;

        public float ParryDuration => parryDuration;
        //[SerializeField] private int dashAmount;
    }
}
