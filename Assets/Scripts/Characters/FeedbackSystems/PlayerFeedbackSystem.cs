using Cameras;
using UnityEngine;

namespace Characters.FeedbackSystems
{
    public class PlayerFeedbackSystem : FeedbackSystem
    {
        [SerializeField] private Cinemachine2DCameraController _cameraController;
        
        public override void PlayFeedback(FeedbackName feedbackName)
        {
            if (!feedbackList.ContainsKey(feedbackName)) return;
            if (ignoreFeedbackList.Contains(feedbackName)) return;
            
            if (feedbackName == FeedbackName.AttackHit)
            {
                
            }
            
            base.PlayFeedback(feedbackName);
        }
    }
}
