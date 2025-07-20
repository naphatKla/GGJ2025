using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.CharacterDataSO
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "GameData/CharacterData/EnemyData")]
    public class EnemyDataSo : BaseCharacterDataSo
    {
        [Title("Exp Data", TitleAlignment = TitleAlignments.Centered)]
        [SerializeField]
        private int expDrop;
        
        /// <summary>
        /// Exp drop after dead
        /// </summary>
        public int ExpDrop => expDrop;
    }
}