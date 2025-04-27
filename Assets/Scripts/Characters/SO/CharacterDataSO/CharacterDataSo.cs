using UnityEngine;

namespace Characters.SO.CharacterDataSO
{
    [CreateAssetMenu(fileName = "CharacterData", menuName = "GameData/CharacterData")]
    public class CharacterDataSo : ScriptableObject
    {
        [SerializeField] private float maxHealth;
        [SerializeField] private float speed;
        [SerializeField] private float attackDamage;

        /// <summary>
        /// The maximum health value for the character.
        /// </summary>
        public float MaxHealth => maxHealth;

        /// <summary>
        /// The base movement speed of the character.
        /// </summary>
        public float Speed => speed;

        /// <summary>
        /// The base attack damage of the character.
        /// </summary>
        public float AttackDamage => attackDamage;
    }
}
