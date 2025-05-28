using System;
using System.Collections.Generic;
using Characters.StatusEffectSystems;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    
    public class SkillPerfectBubbleDataSo : BaseSkillDataSo
    {
        [Serializable]
        public struct PerfectBubbleDashData
        {
            public float dashDuration;
            public float dashDistance;
            public AnimationCurve dashCurve;
            public AnimationCurve dashEase;
            public List<StatusEffectDataPayload> effectOnDash;
        }
        
        [SerializeField] private float counterDuration;
        [SerializeField] private int dashAmount;
        
    }
}
