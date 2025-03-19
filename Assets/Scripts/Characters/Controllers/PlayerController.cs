using System;
using UnityEngine;

namespace Characters.Controllers
{
    public class PlayerController : CharacterController
    {
        public static PlayerController Instant { get; private set; }

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
