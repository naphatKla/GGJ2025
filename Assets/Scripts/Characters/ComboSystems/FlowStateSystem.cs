using System.Collections.Generic;
using Characters.SO.CharacterDataSO;
using UnityEngine;

namespace Characters.ComboSystems
{
    public class FlowStateSystem : MonoBehaviour
    {
        private List<FlowStateData> _flowStateDatas;
        public void AssignData(List<FlowStateData> flowStateDatas)
        {
            _flowStateDatas = flowStateDatas;
        }
    }
}
