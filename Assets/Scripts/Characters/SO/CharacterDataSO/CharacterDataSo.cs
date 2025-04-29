using UnityEngine;
using UnityEngine.Serialization;

namespace Characters.SO.CharacterDataSO
{
    [CreateAssetMenu(fileName = "CharacterData", menuName = "GameData/CharacterData")]
    public class CharacterDataSo : ScriptableObject
    {
        [SerializeField] private float maxHealth;
        [SerializeField] private float baseSpeed;
        [SerializeField] private float baseDamage;

        /// <summary>
        /// The maximum health value for the character.
        /// </summary>
        public float MaxHealth => maxHealth;

        /// <summary>
        /// The base movement speed of the character.
        /// </summary>
        public float BaseSpeed => baseSpeed;

        /// <summary>
        /// The base attack damage of the character.
        /// </summary>
        public float BaseDamage => baseDamage;
    }
}
