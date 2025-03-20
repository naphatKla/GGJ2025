using System;
using UnityEngine;

namespace Characters.InputSystems.Interface
{
    public interface ICharacterInput
    {
        /// <summary>
        /// Event triggered when the player or entity moves.
        /// The Vector2 parameter represents the movement position.
        /// </summary>
        public Action<Vector2> OnMove { get; set; }

        /// <summary>
        /// Event triggered when the primary skill is performed.
        /// The Vector2 parameter represents the target direction of the skill.
        /// </summary>
        public Action<Vector2> OnPrimarySkillPerform { get; set; }

        /// <summary>
        /// Event triggered when the secondary skill is performed.
        /// The Vector2 parameter represents the target direction of the skill.
        /// </summary>
        public Action<Vector2> OnSecondarySkillPerform { get; set; }
    }
}
