using System;
using System.Collections;
using Characters.Controllers;
using Characters.InputSystems.Interface;
using Characters.MovementSystems;
using Characters.SkillSystems;
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
        
        private BaseMovementSystem movementSystem;

        /// <summary>
        /// Coroutine that periodically updates AI behavior.
        /// </summary>
        private Coroutine updateTickCoroutine;

        /// <summary>
        /// Interval (in seconds) between AI update ticks.
        /// Controls how often movement and skill inputs are simulated.
        /// </summary>
        private float timeTick = 0.2f;

        private DirectionContainer _sightDirection;
        
        DirectionContainer ICharacterInput.SightDirection
        {
            get => _sightDirection;
            set => _sightDirection = value;
        }

        /// <inheritdoc/>
        public Action<Vector2> OnMove { get; set; }

        public Action<SkillType> OnSkillPerform { get; set; }

        #endregion

        #region Unity Methods

        /// <summary>
        /// Unity event called when the object becomes enabled and active.
        /// Starts the AI update coroutine.
        /// </summary>
        private void OnEnable()
        {
            updateTickCoroutine = StartCoroutine(UpdateTick());
            movementSystem = GetComponent<BaseMovementSystem>();
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
        /// PrimaryMovement will toggle enable if distance > distance between player and enemy
        /// </summary>
        private IEnumerator UpdateTick()
        {
            bool isMovementEnabled = true;
            
            while (true)
            {
                yield return new WaitForSeconds(timeTick);

                if (!PlayerController.Instance) continue;
                
                _sightDirection.direction  = (PlayerController.Instance.transform.position - transform.position).normalized;
                _sightDirection.length = Vector2.Distance(PlayerController.Instance.transform.position, transform.position);
                
                bool shouldEnable = _sightDirection.length >= 3f;
                if (shouldEnable != isMovementEnabled)
                {
                    movementSystem?.StopFromInput(shouldEnable);
                    isMovementEnabled = shouldEnable;
                }
                
                OnMove?.Invoke(_sightDirection.direction);
                OnSkillPerform?.Invoke(SkillType.PrimarySkill);
                OnSkillPerform?.Invoke(SkillType.SecondarySkill);
            }
        }

        #endregion
    }
}
