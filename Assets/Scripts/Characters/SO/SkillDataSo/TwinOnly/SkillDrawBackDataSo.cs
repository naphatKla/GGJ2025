using System.Collections.Generic;
using Characters.StatusEffectSystems;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo.TwinOnly
{
    [CreateAssetMenu(fileName = "SkillDrawBackData", menuName = "GameData/SkillData/TwinOnly/SkillDrawBackData")]
    public class SkillDrawBackDataSo : BaseSkillDataSo
    {
        [FoldoutGroup("DrawBack Config"), SerializeField]
        private List<StatusEffectDataPayload> effectSelfOnSuccess;

        public List<StatusEffectDataPayload> EffectSelfOnSuccess => effectSelfOnSuccess;
    }
}
