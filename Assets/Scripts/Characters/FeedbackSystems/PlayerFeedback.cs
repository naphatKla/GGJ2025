using UnityEngine;

namespace Characters.FeedbackSystems
{
    public class PlayerFeedback : FeedbackSystem
    {
        public override void PlayFeedback(FeedbackName feedbackName)
        {
            if (!feedbackList.ContainsKey(feedbackName)) return;
            if (ignoreFeedbackList.Contains(feedbackName)) return;
            
            base.PlayFeedback(feedbackName);
        }
    }
}
