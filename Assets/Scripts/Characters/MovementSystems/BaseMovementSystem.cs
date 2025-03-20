using UnityEngine;

namespace Characters.MovementSystems
{
    public abstract class BaseMovementSystem : MonoBehaviour
    {
        #region Inspectors & Variables
        protected float maxSpeed = 6f;
        protected float currentSpeed = 6f;
        private bool _canMove = true;
        #endregion

        #region Methods
        /// <summary>
        /// Try moving with the 'can move' condition.
        /// </summary>
        /// <param name="position"></param>
        public virtual void TryMove(Vector2 position)
        {
            if (!_canMove) return;
            Move(position);
        }
        
        /// <summary>
        /// Moves continuously with current speed to the new position. This method was called in fixed update.
        /// </summary>
        /// <param name="position"></param>
        protected abstract void Move(Vector2 position);
        #endregion
    }
}
