using System;
using UnityEngine;

namespace Characters.InputSystems.Interface
{
    public interface ICharacterInput
    {
        public Action<Vector2> OnMove { get; set; }
        public Action OnPrimarySkillPerform { get; set; }
        public Action OnSecondarySKillPerform { get; set; }
    }
}
