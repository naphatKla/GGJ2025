using System;
using System.Collections;
using Characters.Controllers;
using Characters.InputSystems.Interface;
using UnityEngine;

namespace Characters.InputSystems
{
    /// <summary>
    /// Handles AI-controlled input for enemies, simulating player-like movement and skill usage.
    /// Implements `ICharacterInput` to integrate with the movement and skill systems.
    /// </summary>
    public class EnemyInputReader : MonoBehaviour, ICharacterInput
    {
        #region Events

        /// <summary>
        /// Event triggered to move the enemy.
        /// The Vector2 parameter represents the movement direction or target position.
        /// </summary>
        public Action<Vector2> OnMove { get; set; }

        /// <summary>
        /// Event triggered when the enemy performs the primary skill.
        /// The Vector2 parameter represents the target direction.
        /// </summary>
        public Action<Vector2> OnPrimarySkillPerform { get; set; }

        /// <summary>
        /// Event triggered when the enemy performs the secondary skill.
        /// The Vector2 parameter represents the target direction.
        /// </summary>
        public Action<Vector2> OnSecondarySkillPerform { get; set; }

        #endregion

        #region Variables

        /// <summary>
        /// Coroutine responsible for updating AI movement at a fixed interval.
        /// </summary>
        private Coroutine updateTickCoroutine;

        /// <summary>
        /// Time interval (in seconds) between AI movement updates.
        /// </summary>
        private float timeTick = 0.2f;

        #endregion

        #region Unity Methods

        /// <summary>
        /// Called when the script is enabled. Starts the AI update tick coroutine.
        /// </summary>
        private void OnEnable()
        {
            updateTickCoroutine = StartCoroutine(UpdateTick());
        }

        /// <summary>
        /// Called when the script is disabled. Stops the AI update tick coroutine.
        /// </summary>
        private void OnDisable()
        {
            if (updateTickCoroutine == null) return;
            StopCoroutine(updateTickCoroutine);
        }

        #endregion

        #region AI Logic

        /// <summary>
        /// Continuously updates the AI movement at fixed time intervals.
        /// The enemy moves towards the player's position every `timeTick` seconds.
        /// </summary>
        private IEnumerator UpdateTick()
        {
            while (true)
            {
                yield return new WaitForSeconds(timeTick);

                // Move enemy toward the player's position
                if (!PlayerController.Instant) continue;
                OnMove?.Invoke(PlayerController.Instant.transform.position);
            }
        }

        #endregion
    }
}
