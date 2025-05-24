using System.Collections.Generic;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace Characters.FeedbackSystems
{
    /// <summary>
    /// Centralized feedback handler attached to a character.
    /// Stores a dictionary of named MMF_Player feedbacks and provides public methods 
    /// to trigger or stop them via enum keys. Designed for shared access by scripts 
    /// within the character’s behavior system (e.g., skills, movement, status effects).
    /// </summary>
    public class FeedbackSystem : SerializedMonoBehaviour
    {
        #region Inspector & Variables

        [PropertyTooltip("List of feedbacks mapped by enum keys. Used to play/stop feedbacks at runtime.")]
        [DictionaryDrawerSettings(KeyLabel = "Feedback Name", ValueLabel = "MMF_Player Reference")]
        [OdinSerialize]
        private Dictionary<FeedbackName, MMF_Player> _feedbackList;

        #endregion

        #region Methods

        /// <summary>
        /// Plays the feedback associated with the given key, if available.
        /// Typically used by other character systems like skills, movement, or damage events.
        /// </summary>
        /// <param name="feedbackName">The key representing the feedback to play.</param>
        public void PlayFeedback(FeedbackName feedbackName)
        {
            if (!_feedbackList.ContainsKey(feedbackName)) return;
            _feedbackList[feedbackName]?.PlayFeedbacks();
        }

        /// <summary>
        /// Stops the feedback associated with the given key, if currently playing.
        /// </summary>
        /// <param name="feedbackName">The key representing the feedback to stop.</param>
        public void StopFeedback(FeedbackName feedbackName)
        {
            if (!_feedbackList.ContainsKey(feedbackName)) return;
            _feedbackList[feedbackName]?.StopFeedbacks();
        }
        
        #endregion
    }

    /// <summary>
    /// Enum representing all named feedback events used in the game.
    /// Used as dictionary keys for triggering visual/audio feedbacks in the character.
    /// </summary>
    public enum FeedbackName
    {
        None = 0,
        Dash = 1,
        BlackHole = 2,
    }
}
