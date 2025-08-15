using UnityEngine;

namespace Characters.SO.ComboStreakDataSO.StageDataSO
{
    [CreateAssetMenu(menuName = "GameData/ComboStreak/StageData/FlowStageData")]
    public class FlowStageSo : BaseComboStageSo 
    {
        public override void OnEnter(StageContext ctx)
        {
            if (stageId == "flow_i")
                ctx.System.FlowStageI++;
            if (stageId == "flow_ii")
                ctx.System.FlowStageII++;
            
            base.OnEnter(ctx);
        }

        public override void OnExit(StageContext ctx)
        {
            if (stageId == "flow_i")
                ctx.System.FlowStageI--;
            if (stageId == "flow_ii")
                ctx.System.FlowStageII--;
            
            base.OnExit(ctx);
        }
    }
}
