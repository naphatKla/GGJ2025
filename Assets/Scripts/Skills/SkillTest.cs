using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillTest : SkillBase
{
    public override void UseSkill()
    {
        OwnerCharacter.AdjustSize(1);
    }
}
