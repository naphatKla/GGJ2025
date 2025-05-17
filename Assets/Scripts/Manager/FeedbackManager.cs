using System.Collections.Generic;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Manager
{
    /// <summary>
    /// Centralized manager for playing and stopping MMFeedbacks across the game using a dictionary-based lookup.
    /// Listens to global events and dispatches the corresponding feedback player.
    /// Helps decouple gameplay logic from visual/audio effects and simplifies feedback management.
    /// </summary>
    public class FeedbackManager : SerializedMonoBehaviour
    {
        #region Inspector & Variables
        
        [PropertyTooltip("Maps a feedback name to its corresponding MMF_Player instance. Used for playing/stopping feedbacks via event triggers.")]
        [DictionaryDrawerSettings(KeyLabel = "Feedback Name", ValueLabel = "MMF_Player Reference")]
        [OdinSerialize] 
        private Dictionary<FeedbackName, MMF_Player> _feedbackList;
        
        #endregion

        #region Unity Methods
        
        /// <summary>
        /// Registers event listeners for playing and stopping feedbacks on enable.
        /// </summary>
        private void OnEnable()
        {
            EventManager<FeedbackName>.RegisterEvent(EventKey.PlayFeedback, PlayFeedback);
            EventManager<FeedbackName>.RegisterEvent(EventKey.StopFeedback, StopFeedback);
        }

        /// <summary>
        /// Unregisters event listeners to prevent memory leaks on disable.
        /// </summary>
        private void OnDisable()
        {
            EventManager<FeedbackName>.UnRegisterEvent(EventKey.PlayFeedback, PlayFeedback);
            EventManager<FeedbackName>.UnRegisterEvent(EventKey.StopFeedback, StopFeedback);
        }
        
        #endregion

        #region Methods
        
        /// <summary>
        /// Plays the MMF_Player associated with the provided feedback name.
        /// </summary>
        /// <param name="feedbackName">The identifier of the feedback to play.</param>
        private void PlayFeedback(FeedbackName feedbackName)
        {
            if (!_feedbackList.ContainsKey(feedbackName)) return;
            _feedbackList[feedbackName]?.PlayFeedbacks();
        }

        /// <summary>
        /// Stops the MMF_Player associated with the provided feedback name.
        /// </summary>
        /// <param name="feedbackName">The identifier of the feedback to stop.</param>
        private void StopFeedback(FeedbackName feedbackName)
        {
            if (!_feedbackList.ContainsKey(feedbackName)) return;
            _feedbackList[feedbackName]?.StopFeedbacks();
        }
        
        #endregion
    }

    /// <summary>
    /// Enumeration representing all feedback keys used across the project.
    /// This ensures consistency and helps avoid string-based errors.
    /// </summary>
    public enum FeedbackName
    {
        None = 0,
        PlayerDash = 1,
        EnemyDash = 2,
    }
}
