using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

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

        [SerializeField]
        [PropertyTooltip("Multiplier applied to bounce speed when the character collides during tween-based movement.")]
        private float bounceMultiplier = 1f;

        [SerializeField]
        [PropertyTooltip("Mass factor used in physics-based interactions, such as knockback or bounce weighting.")]
        private float mass = 1f;

        [Title("Combat Data", TitleAlignment = TitleAlignments.Centered)]
        [SerializeField]
        [PropertyTooltip("The base damage this character deals per hit.")]
        private float baseDamage;


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
        /// Multiplier applied to bounce force during collisions while moving via tween.
        /// Typically used to control how far the character rebounds after impact.
        /// </summary>
        public float BounceMultiplier => bounceMultiplier;

        /// <summary>
        /// The effective mass of the character used for calculating momentum,
        /// knockback resistance, and bounce responsiveness.
        /// </summary>
        public float Mass => mass;

        /// <summary>
        /// The base attack damage of the character.
        /// </summary>
        public float BaseDamage => baseDamage;

        #endregion
    }
}