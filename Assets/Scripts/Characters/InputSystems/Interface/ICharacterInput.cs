using System;
using Characters.SkillSystems;
using UnityEngine;

namespace Characters.InputSystems.Interface
{
    /// <summary>
    /// Interface for handling character input events such as movement and skill usage.
    /// Implemented by input readers for both player and AI-controlled characters.
    /// </summary>
    public interface ICharacterInput
    {
        public DirectionContainer SightDirection { get; protected set; }
        public bool Enable { get; set; }
        
        /// <summary>
        /// Invoked when a movement input is received.
        /// The Vector2 parameter represents the movement direction
        /// </summary>
        Action<Vector2> OnMove { get; set; }

        Action<SkillType> OnSkillPerform { get; set; }
    }

    public struct DirectionContainer
    {
        public Vector2 direction;
        public float length;
    }
}