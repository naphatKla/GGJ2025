namespace Characters.Controllers
{
    /// <summary>
    /// The player-specific implementation of <see cref="BaseController"/>.
    /// Handles player-related logic such as singleton access to the current player instance.
    /// </summary>
    public class PlayerController : BaseController
    {
        #region Inspector & Variables
        
        /// <summary>
        /// A global static reference to the current player instance.
        /// Allows other systems to easily access the active player in the scene.
        /// </summary>
        public static PlayerController Instance { get; private set; }
        
        #endregion

        #region Unity Methods
        
        /// <summary>
        /// Called when the script instance is being loaded.
        /// Ensures that only one instance of PlayerController exists in the scene.
        /// If another exists, it is destroyed.
        /// </summary>
        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                return;
            }
        }
        
        #endregion
    }
}