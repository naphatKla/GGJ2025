using System;
using UnityEngine;

namespace Characters.Controllers
{
    /// <summary>
    /// The player-specific implementation of <see cref="BaseController"/>.
    /// Handles player-related logic such as singleton access to the current player instance.
    /// </summary>
    public class PlayerController : BaseController
    {
        /// <summary>
        /// A global static reference to the current player instance.
        /// Allows other systems to easily access the active player in the scene.
        /// </summary>
        public static PlayerController Instant { get; private set; }

        /// <summary>
        /// Called when the script instance is being loaded.
        /// Ensures that only one instance of PlayerController exists in the scene.
        /// If another exists, it is destroyed.
        /// </summary>
        private void Awake()
        {
            if (!Instant)
            {
                Instant = this;
                return;
            }

            Destroy(gameObject);
        }
    }
}