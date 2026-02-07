using UnityEngine;
using System;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Handles the core state machine and time progression logic (The "Brain").
    /// Controlled by SessionManager or Debug buttons.
    /// Manipulates the GameManager data.
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        [SerializeField] private GameManager gameManager;

        // -------------------------------------------------------------------------
        // Logic
        // -------------------------------------------------------------------------
        public void Initialize(GameManager manager)
        {
            gameManager = manager;
        }

        public void StartNewGame()
        {
            if (gameManager == null) return;
            
            // Reset Session Data
            gameManager.SessionData = new GameSessionData
            {
                CurrentDay = 1,
                CurrentState = GameState.StatusReview,
                IsGameOver = false,
                FamilyCount = 4, // Default starting family
                AverageHealth = 100f // Default health
            };
            
            Debug.Log($"[Sim] GAME START | Day: {gameManager.CurrentDay} | Family: {gameManager.SessionData.FamilyCount} | Health: {gameManager.SessionData.AverageHealth}%");
            
            GameManager.FireDayStart();
        }

        public void SetState(GameState newState)
        {
            if (gameManager == null || gameManager.IsGameOver) return;
            if (gameManager.CurrentState == newState) return;

            gameManager.CurrentState = newState;
            Debug.Log($"[GameFlow] State -> {newState}");
            
            GameManager.FireStateChanged(newState);

            if (newState == GameState.StatusReview)
            {
                GameManager.FireDayStart();
            }
            else if (newState == GameState.NightCycle)
            {
                GameManager.FireNightStart();
            }
        }

        public void AdvanceDay()
        {
            if (gameManager == null) return;

            // Increment Day
            gameManager.CurrentDay++;
            
            // Simulation Logic (Placeholder or read from managers)
            // In a real scenario, this would aggregate data from SurvivalManager/FamilyManager
            if (gameManager.Family != null)
            {
                // If FamilyManager exists, use its data (Future implementation)
                // gameManager.SessionData.FamilyCount = gameManager.Family.GetAliveCount();
            }
            else
            {
                // Simple Simulation for testing: Randomly decrease health slightly
                gameManager.SessionData.AverageHealth = Mathf.Max(0, gameManager.SessionData.AverageHealth - UnityEngine.Random.Range(0f, 5f));
            }

            Debug.Log($"[Sim] ADVANCE DAY | Day: {gameManager.CurrentDay} | Family: {gameManager.SessionData.FamilyCount} | Health: {gameManager.SessionData.AverageHealth:F1}%");

            var config = GameConfigDataSO.Instance;
            if (config != null && gameManager.CurrentDay > config.TotalDays)
            {
                EndGame(true);
            }
        }

        public void PreviousDay()
        {
            if (gameManager == null || gameManager.CurrentDay <= 1) return;
            
            gameManager.CurrentDay--;
            Debug.Log($"[GameFlow] Time Travel -> Day {gameManager.CurrentDay}");
            GameManager.FireDayStart();
        }

        public void EndGame(bool survived)
        {
            if (gameManager == null) return;

            gameManager.IsGameOver = true;
            Debug.Log($"[GameFlow] Game Over. Survived: {survived}");
            GameManager.FireGameOver(survived);
        }

        // -------------------------------------------------------------------------
        // Debug
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug Controls")]
        [Button("Start New Game", ButtonSizes.Large)]
        [GUIColor(0f, 1f, 0f)]
        private void Debug_StartNewGame() => StartNewGame();

        [Button("Force Advance Day", ButtonSizes.Medium)]
        private void Debug_Advance() => AdvanceDay();
        #endif
    }
}
