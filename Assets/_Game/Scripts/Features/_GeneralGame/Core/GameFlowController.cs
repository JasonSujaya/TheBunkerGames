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
        [SerializeField] private DayTransitionUI dayTransitionUI;
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
                gameManager.SessionData.UpdateSync(gameManager.Family, gameManager.Inventory); // Early sync for inspector
            }

            // Start locally at Day 0 so AdvanceDay() brings us to Day 1
            gameManager.CurrentDay = 0;
            
            if (enableDebugLogs) Debug.Log($"[Sim] GAME INITIALIZATION | Family: {gameManager.SessionData.FamilyCount}");
            
            // Spawn Default Family & Items (One-time Setup)
            if (gameManager.Family != null)
            {
                // Always spawn default family (which now contains the user selection from FamilySelectUI)
                // We clear existing first to be safe
                gameManager.Family.ClearFamily();
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

            // Refresh HUD now that family is spawned
            var hud = gameManager.GameplayHud != null ? gameManager.GameplayHud : GameplayHudUI.Instance;
            if (hud != null)
            {
                hud.RefreshAll();
            }
                
            // Now advance to Day 1 which triggers all refreshes, events, and action preparation
            AdvanceDay();
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
            }

            public void AdvanceDay(bool skipTransition = false)
            {
                if (gameManager == null) return;

                int fromDay = gameManager.CurrentDay;
                int toDay = fromDay + 1;

                // Skip transition for initial game start (Day 0 -> Day 1) or if explicitly requested
                if (skipTransition || fromDay == 0)
                {
                    ExecuteDayAdvance();
                    return;
                }

                // Show transition screen if available
                if (dayTransitionUI != null)
                {
                    // Enable the GameObject and set Instance
                    dayTransitionUI.gameObject.SetActive(true);
                    
                    if (enableDebugLogs) Debug.Log($"[GameFlow] Showing transition: Day {fromDay} -> Day {toDay}");
                    
                    dayTransitionUI.ShowTransition(
                        fromDay, 
                        toDay, 
                        "Day is transitioning...",
                        () => ExecuteDayAdvance()
                    );
                }
                else
                {
                    // No transition UI, execute immediately
                    ExecuteDayAdvance();
                }
            }

            /// <summary>
            /// Executes the actual day advancement logic.
            /// Called directly or after transition animation completes.
            /// </summary>
            private void ExecuteDayAdvance()
            {
                // Submit current day's actions before advancing
                if (PlayerActionManager.Instance != null && gameManager.CurrentDay > 0)
                {
                    Debug.Log($"[GameFlow] Calling SubmitAllActions for Day {gameManager.CurrentDay}...");
                    PlayerActionManager.Instance.SubmitAllActions();
                }

                // Increment Day
                gameManager.CurrentDay++;
                
                // 1. Fire Day Start Event (Triggers SurvivalManager decay, etc)
                GameManager.FireDayStart();

                // 2. Prepare Daily Actions for the new day
                if (gameManager.Actions != null)
                {
                    gameManager.Actions.PrepareDailyActions(gameManager.CurrentDay);
                }

                // Refresh HUD completely (characters, bodies, resources)
                var hud = gameManager.GameplayHud != null ? gameManager.GameplayHud : GameplayHudUI.Instance;
                if (hud != null)
                {
                    hud.RefreshAll();
                }

                // 2b. Force refresh PlayerActionUI directly (bypasses event subscription issues)
                if (PlayerActionUI.Instance != null)
                {
                    PlayerActionUI.Instance.RefreshForNewDay();
                }

                // 3. Sync Data for Visualization
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
