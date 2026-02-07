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
        [SerializeField] private bool autoStartGame = true;
        [SerializeField] private bool enableDebugLogs = false;

        // -------------------------------------------------------------------------
        // Logic
        // -------------------------------------------------------------------------
        public void Initialize(GameManager manager)
        {
            gameManager = manager;
        }

        private void Start()
        {
            if (autoStartGame)
            {
                StartNewGame();
            }
        }

        public void StartNewGame()
        {
            if (gameManager == null) return;
            
            // Reset Session Data
            if (gameManager.SessionData != null)
            {
                gameManager.SessionData.ResetData();
            }
            
            if (enableDebugLogs) Debug.Log($"[Sim] GAME START | Day: {gameManager.CurrentDay} | Family: {gameManager.SessionData.FamilyCount} | Health: {gameManager.SessionData.AverageHealth}%");
            
                // Spawn Default Family & Items
                if (gameManager.Family != null)
                {
                    gameManager.Family.SpawnDefaultFamily();
                    // Sync only family first
                    gameManager.SessionData.UpdateSync(gameManager.Family);

                    // Add Starting Items from profile
                    var profile = gameManager.Family.DefaultFamilyProfile;
                    if (profile != null && gameManager.Inventory != null)
                    {
                        gameManager.Inventory.ClearInventory();
                        if (profile.StartingItems != null)
                        {
                            foreach (var entry in profile.StartingItems)
                            {
                                if (entry.Item != null && entry.Quantity > 0)
                                {
                                    gameManager.Inventory.AddItem(entry.Item, entry.Quantity);
                                }
                            }
                        }
                        if (enableDebugLogs) Debug.Log($"[Sim] Added starting items from {profile.name}");
                    }
                }
                
                // Full Sync (Family + Inventory)
                gameManager.SessionData.UpdateSync(gameManager.Family, gameManager.Inventory);

                GameManager.FireDayStart();
            }

            public void SetState(GameState newState)
            {
                if (gameManager == null || gameManager.IsGameOver) return;
                if (gameManager.CurrentState == newState) return;

                gameManager.CurrentState = newState;
                if (enableDebugLogs) Debug.Log($"[GameFlow] State -> {newState}");
                
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
                
                // 1. Fire Day Start Event (Triggers SurvivalManager decay, etc)
                GameManager.FireDayStart();

                // 2. Sync Data for Visualization
                if (gameManager.SessionData != null)
                {
                    // Sync with Family Manager & Inventory if available
                    gameManager.SessionData.UpdateSync(gameManager.Family, gameManager.Inventory);
                    
                    if (gameManager.Family == null)
                    {
                        // Fallback Simulation if no family manager found
                         gameManager.SessionData.AverageHealth = Mathf.Max(0, gameManager.SessionData.AverageHealth - UnityEngine.Random.Range(0f, 5f));
                    }
                }

                if (enableDebugLogs) Debug.Log($"[Sim] ADVANCE DAY | Day: {gameManager.CurrentDay} | Family: {gameManager.SessionData.FamilyCount} | Health: {gameManager.SessionData.AverageHealth:F1}%");

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
            if (enableDebugLogs) Debug.Log($"[GameFlow] Time Travel -> Day {gameManager.CurrentDay}");
            GameManager.FireDayStart();
        }

        public void EndGame(bool survived)
        {
            if (gameManager == null) return;

            gameManager.IsGameOver = true;
            if (enableDebugLogs) Debug.Log($"[GameFlow] Game Over. Survived: {survived}");
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
