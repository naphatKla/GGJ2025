using System;
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
        }
        
        [SerializeField] private float counterDuration;
        [SerializeField] private int dashAmount;
        
    }
}
