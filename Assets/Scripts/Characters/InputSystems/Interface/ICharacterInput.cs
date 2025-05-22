using System;
using UnityEngine;

namespace Characters.InputSystems.Interface
{
    /// <summary>
    /// Interface for handling character input events such as movement and skill usage.
    /// Implemented by input readers for both player and AI-controlled characters.
    /// </summary>
    public interface ICharacterInput
    {
        /// <summary>
        /// Invoked when a movement input is received.
        /// The Vector2 parameter represents the movement direction
        /// </summary>
        Action<Vector2> OnMove { get; set; }

        /// <summary>
        /// Invoked when the primary skill input is triggered.
        /// The Vector2 parameter represents the direction or aim of the skill.
        /// </summary>
        Action<Vector2> OnPrimarySkillPerform { get; set; }

        /// <summary>
        /// Invoked when the secondary skill input is triggered.
        /// The Vector2 parameter represents the direction or aim of the skill.
        /// </summary>
        Action<Vector2> OnSecondarySkillPerform { get; set; }
    }
}