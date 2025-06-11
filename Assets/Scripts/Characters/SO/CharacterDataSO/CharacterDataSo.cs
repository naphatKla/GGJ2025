using System.Collections.Generic;
using Characters.StatusEffectSystems;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.CharacterDataSO
{
    [CreateAssetMenu(fileName = "CharacterData", menuName = "GameData/CharacterData")]
    public class CharacterDataSo : ScriptableObject
    {
        #region Inspector & Variables

        [Title("Health Data", TitleAlignment = TitleAlignments.Centered)]
        [PropertyTooltip("The maximum health value the character can have.")]
        [SerializeField]
        private float maxHealth;

        [PropertyTooltip(
            "Duration of temporary invincibility (in seconds) after the character takes damage. Prevents consecutive hits during this period.")]
        [SerializeField]
        private float invincibleTimePerHit = 0.1f;

        [Title("Movement Data", TitleAlignment = TitleAlignments.Centered)]
        [PropertyTooltip("The base movement speed of the character when moving at full speed. (Units/Sec)")]
        [SerializeField]
        private float baseSpeed;

        [SerializeField] [PropertyTooltip("How quickly the character accelerates toward the target movement speed.")]
        private float moveAccelerationRate;

        [SerializeField] [PropertyTooltip("How quickly the character adjusts its movement direction.")]
        private float turnAccelerationRate;
        
        [Title("Combat Data", TitleAlignment = TitleAlignments.Centered)]
        [SerializeField]
        [PropertyTooltip("The base damage this character deals per hit.")]
        private float baseDamage;

        [Title("Exp Data", TitleAlignment = TitleAlignments.Centered)]
        [SerializeField]
        private int expDrop;


        /// <summary>
        /// The maximum health value for the character.
        /// </summary>
        public float MaxHealth => maxHealth;

        /// <summary>
        /// Duration of temporary invincibility (in seconds) after the character takes damage. Prevents consecutive hits during this period.
        /// </summary>
        public float InvincibleTimePerHit => invincibleTimePerHit;

        /// <summary>
        /// The base movement speed of the character.
        /// </summary>
        public float BaseSpeed => baseSpeed;

        /// <summary>
        /// The rate at which the character accelerates toward its movement speed.
        /// </summary>
        public float MoveAccelerationRate => moveAccelerationRate;

        /// <summary>
        /// The rate at which the character adjusts its movement direction.
        /// </summary>
        public float TurnAccelerationRate => turnAccelerationRate;
        
        /// <summary>
        /// Exp drop after dead
        /// </summary>
        public int ExpDrop => expDrop;
        
        /// <summary>
        /// The base attack damage of the character.
        /// </summary>
        public float BaseDamage => baseDamage;

        #endregion
    }
}