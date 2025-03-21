using System;
using System.Collections;
using Characters.Controllers;
using Characters.InputSystems.Interface;
using UnityEngine;

namespace Characters.InputSystems
{
    /// <summary>
    /// Simulates input for AI-controlled enemies.
    /// Implements <see cref="ICharacterInput"/> to integrate with the movement and skill systems,
    /// mimicking player input behavior on a timed loop.
    /// </summary>
    public class EnemyInputReader : MonoBehaviour, ICharacterInput
    {
        #region Inspector & Variables

        /// <summary>
        /// Coroutine that periodically updates AI behavior.
        /// </summary>
        private Coroutine updateTickCoroutine;

        /// <summary>
        /// Interval (in seconds) between AI update ticks.
        /// Controls how often movement and skill inputs are simulated.
        /// </summary>
        private float timeTick = 0.2f;

        /// <inheritdoc/>
        public Action<Vector2> OnMove { get; set; }

        /// <inheritdoc/>
        public Action<Vector2> OnPrimarySkillPerform { get; set; }

        /// <inheritdoc/>
        public Action<Vector2> OnSecondarySkillPerform { get; set; }

        #endregion

        #region Unity Methods

        /// <summary>
        /// Unity event called when the object becomes enabled and active.
        /// Starts the AI update coroutine.
        /// </summary>
        private void OnEnable()
        {
            updateTickCoroutine = StartCoroutine(UpdateTick());
        }

        /// <summary>
        /// Unity event called when the object becomes disabled or inactive.
        /// Stops the AI update coroutine.
        /// </summary>
        private void OnDisable()
        {
            if (updateTickCoroutine == null) return;
            StopCoroutine(updateTickCoroutine);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Periodically updates AI behavior every <c>timeTick</c> seconds.
        /// Simulates movement toward the player and triggers skill input.
        /// </summary>
        private IEnumerator UpdateTick()
        {
            while (true)
            {
                yield return new WaitForSeconds(timeTick);

                if (!PlayerController.Instant) continue;

                Vector2 sightDirection = (PlayerController.Instant.transform.position - transform.position).normalized;

                // Simulate inputs
                OnMove?.Invoke(PlayerController.Instant.transform.position);
                OnPrimarySkillPerform?.Invoke(sightDirection);
                OnSecondarySkillPerform?.Invoke(sightDirection);
            }
        }

        #endregion
    }
}
