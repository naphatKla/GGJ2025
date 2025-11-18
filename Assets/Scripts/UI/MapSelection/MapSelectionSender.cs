using Challenge.Challenge;
using MoreMountains.Tools;
using ProjectExtensions;
using Sirenix.OdinInspector;

namespace UI.MapSelection
{
    public class MapSelectionSender : AutoCreatePersistentSingleton<MapSelectionSender>
    {
        public int currentMapSelectionIndex = 0;
        public MapSelectionDataContainer currentmapSelectionDataContainer;
        [ShowInInspector,ReadOnly] public ChallengeSnapshot challengeData;
        
        public void UpdateChallengeData(ChallengeSnapshot snap)
        {
            challengeData = snap;
        }
    }
}