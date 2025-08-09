using System.Collections.Generic;
using Characters.Controllers;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

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
        protected Dictionary<FeedbackName, MMF_Player> feedbackList;
        protected readonly HashSet<FeedbackName> ignoreFeedbackList = new();

        [SerializeField] private TrailRenderer trail;
        private BaseController _owner;

        #endregion

        #region Methods

        public virtual void AssignData(BaseController owner)
        {
            _owner = owner;
        }
        
        /// <summary>
        /// Plays the feedback associated with the given key, if available.
        /// Typically used by other character systems like skills, movement, or damage events.
        /// </summary>
        /// <param name="feedbackName">The key representing the feedback to play.</param>
        public virtual void PlayFeedback(FeedbackName feedbackName)
        {
            if (!feedbackList.ContainsKey(feedbackName)) return;
            if (ignoreFeedbackList.Contains(feedbackName)) return;
            feedbackList[feedbackName]?.PlayFeedbacks();
        }

        /// <summary>
        /// Stops the feedback associated with the given key, if currently playing.
        /// </summary>
        /// <param name="feedbackName">The key representing the feedback to stop.</param>
        public virtual void StopFeedback(FeedbackName feedbackName)
        {
            if (!feedbackList.ContainsKey(feedbackName)) return;
            feedbackList[feedbackName]?.StopFeedbacks();
        }

        public virtual bool IsFeedbackPlaying(FeedbackName feedbackName)
        {
            if (!feedbackList.ContainsKey(feedbackName)) return false;
            return feedbackList[feedbackName].IsPlaying;
        }

        public virtual void SetIgnoreFeedback(FeedbackName feedbackName, bool isIgnore)
        {
            if (isIgnore)
            {
                ignoreFeedbackList.Add(feedbackName);
                return;
            }

            ignoreFeedbackList.Remove(feedbackName);
        }

        public void ShowTrail(bool enable)
        {
            if (!trail) return;
            trail.Clear();
            trail.ResetBounds();
            trail.ResetLocalBounds();
            trail.emitting = enable;
        }

        public void ResetFeedbackSystem()
        {
            trail.Clear();
            trail.ResetBounds();
            trail.ResetLocalBounds();
        }
        
        #endregion
    }

    /// <summary>
    /// Enum representing all named feedback events used in the game.
    /// Used as dictionary keys for triggering visual/audio feedbacks in the character.
    /// </summary>
    public enum FeedbackName
    {
        None = -100,
        TakeDamage = -99,
        Heal = -98,
        Iframe = -97,
        Dead = -96,
        Spawn = -95,
        Bounce = -94,
        AttackHit = -93,
        CounterAttack = -92,
        Charge = -91,
        
        // skill
        Dash = 1,
        Reflection = 2,
        ParryUSe = 3,
        ParrySuccess = 4,
        LightStepUse = 5,
        LightStepEnd = 6,
        HarmonyOfLight = 7,
    }
}
