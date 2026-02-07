using System;
using UnityEngine;

namespace TheBunkerGames
{
    [Serializable]
    public class GameSessionData
    {
        public GameState CurrentState;
        public int CurrentDay;
        public int FamilyCount;
        public float AverageHealth;
        public bool IsGameOver;
    }
}
