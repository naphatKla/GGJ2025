using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Characters
{
    public class CloningCharacter : CharacterBase
    {
        public float SizeGained;
        public CharacterBase OwnerCharacter;
        protected override void SkillInputHandler() { }

        public override void AddSize(float size)
        {
            base.AddSize(size);
            SizeGained += size;
        }
    }
}
