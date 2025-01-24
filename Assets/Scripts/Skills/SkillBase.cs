using System;
using Characters;
using UnityEngine;

public abstract class SkillBase
{ 
    [SerializeField] private float cooldown = 1f;
    protected CharacterBase OwnerCharacter;
    
    public void InitializeSkill(CharacterBase ownerCharacter)
    {
        OwnerCharacter = ownerCharacter;
    }
    
    /// <summary>
    /// Override this method to implement the skill logic
    /// </summary>
    public abstract void UseSkill();
}
