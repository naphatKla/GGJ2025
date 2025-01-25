
using System;

namespace Characters
{
    public class CloningCharacter : CharacterBase
    {
        public CharacterBase OwnerCharacter;
        protected override void SkillInputHandler() { }
        
        private void OnDestroy()
        {
            if (OwnerCharacter) OwnerCharacter.clones.Remove(this);
        }
    }
}
