using System;
using UnityEngine;

namespace TheBunkerGames
{
    public class GameSessionData : MonoBehaviour
    {
        public GameState CurrentState;
        public int CurrentDay;
        public int FamilyCount;
        public float AverageHealth;
        public bool IsGameOver;

        public void ResetData()
        {
            CurrentDay = 1;
            CurrentState = GameState.StatusReview;
            IsGameOver = false;
            FamilyCount = 4;
            AverageHealth = 100f;
        }
    }
}
