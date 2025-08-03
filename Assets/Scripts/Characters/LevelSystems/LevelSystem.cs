using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.LevelSystems
{
    public class LevelSystem : MonoBehaviour
    {
        public int Level { get; private set; } = 1;
        public int Exp { get; private set; } = 0;

        public event Action<int> OnLevelUp; // int = new level

        [Header("Level Up EXP")]
        public int expPerLevel = 10;
        
        [Button]
        public void AddExp(int amount)
        {
            Exp += amount;
            
            while (Exp >= expPerLevel)
            {
                Exp -= expPerLevel;
                Level++;
                OnLevelUp?.Invoke(Level);
            }
        }

        public void ResetLevel()
        {
            Level = 1;
            Exp = 0;
        }
    }
}