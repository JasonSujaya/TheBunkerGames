using UnityEngine;
using System;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Orchestrates the daily player action system.
    /// Rolls category availability, draws challenges from the pool,
    /// receives player inputs from UI, delegates to LLM bridge,
    /// and executes resulting effects.
    /// </summary>
    public class PlayerActionManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static PlayerActionManager Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------
        /// <summary>Fired when daily actions are prepared and ready for UI.</summary>
        public static event Action<DailyActionState> OnDailyActionsReady;

        /// <summary>Fired when a single category result is received from the LLM.</summary>
        public static event Action<PlayerActionResult> OnCategoryResultReceived;

        /// <summary>Fired when ALL active categories have been completed for the day.</summary>
        public static event Action OnAllActionsComplete;

        /// <summary>Fired when a single category's input has been saved (but not yet submitted to LLM).</summary>
        public static event Action<PlayerActionCategory> OnCategorySaved;

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Configuration")]
        #endif
        [Header("Challenge Pool")]
        [SerializeField] private PlayerActionChallengePoolSO challengePool;

        [Header("Story Log")]
        [SerializeField] private StoryLogSO storyLog;

        #if ODIN_INSPECTOR
        [Title("Availability Settings")]
        [InfoBox("Exploration is always active. Dilemma and Family Request have a random chance per day.")]
        #endif
        [Header("Category Availability")]
        [Range(0f, 1f)]
        [SerializeField] private float dilemmaChance = 0.6f;
        [Range(0f, 1f)]
        [SerializeField] private float familyRequestChance = 0.4f;

        [Header("Game Settings")]
        [SerializeField] private int totalDays = 30;

        #if ODIN_INSPECTOR
        [Title("State")]
        [InfoBox("Shows which categories are active for today. Prepare Day Actions to roll availability.")]
        [ReadOnly, LabelText("Exploration Active")]
        #endif
        [Header("State")]
        [SerializeField] private bool explorationActive;

        #if ODIN_INSPECTOR
        [ReadOnly, LabelText("Dilemma Active")]
        #endif
        [SerializeField] private bool dilemmaActive;

        #if ODIN_INSPECTOR
        [ReadOnly, LabelText("Family Request Active")]
        #endif
        [SerializeField] private bool familyRequestActive;

        #if ODIN_INSPECTOR
        [ReadOnly, LabelText("Family Target")]
        #endif
        [SerializeField] private string familyRequestTargetDisplay = "";

        #if ODIN_INSPECTOR
        [ReadOnly, LabelText("Processing")]
        #endif
        [SerializeField] private bool isProcessingDisplay;

        #if ODIN_INSPECTOR
        [Title("Debug")]
        #endif
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        // -------------------------------------------------------------------------
        // Runtime State
        // -------------------------------------------------------------------------
        private DailyActionState currentDayState;
        private Dictionary<PlayerActionCategory, PlayerActionResult> dayResults = new Dictionary<PlayerActionCategory, PlayerActionResult>();
        private int pendingResults;
        private bool isProcessing;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public DailyActionState CurrentDayState => currentDayState;
        public bool IsProcessing => isProcessing;
        public Dictionary<PlayerActionCategory, PlayerActionResult> DayResults => dayResults;

        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Register the challenge pool singleton
            if (challengePool != null)
                PlayerActionChallengePoolSO.SetInstance(challengePool);
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Prepare daily actions for a given day.
        /// Rolls availability, draws challenges, resets state.
        /// Call this at the start of each day.
        /// </summary>
        public void PrepareDailyActions(int day)
        {
            if (challengePool == null)
            {
                Debug.LogError("[PlayerActionManager] ChallengePool is not assigned!");
                return;
            }

            // Reset
            dayResults.Clear();
            pendingResults = 0;
            isProcessing = false;
            
            // Clear storyteller event queue from previous day
            var storytellerUI = FindFirstObjectByType<StorytellerUI>();
            if (storytellerUI != null)
                storytellerUI.ClearEventQueue();

            currentDayState = new DailyActionState
            {
                Day = day,
                ExplorationActive = true,
                DilemmaActive = UnityEngine.Random.value <= dilemmaChance,
                FamilyRequestActive = ShouldActivateFamilyRequest()
            };

            // Draw challenges
            currentDayState.ExplorationChallenge = challengePool.GetRandomExploration();

            if (currentDayState.DilemmaActive)
                currentDayState.DilemmaChallenge = challengePool.GetRandomDilemma();

            if (currentDayState.FamilyRequestActive)
            {
                currentDayState.FamilyRequestChallenge = challengePool.GetRandomFamilyRequest();
                currentDayState.FamilyRequestTarget = PickFamilyRequestTarget();
            }

            RefreshStateDisplay();

            if (enableDebugLogs)
            {
                var active = currentDayState.GetActiveCategories();
                Debug.Log($"[PlayerActionManager] Day {day} actions prepared: {string.Join(", ", active)} ({active.Count} active)");
                if (currentDayState.ExplorationChallenge != null)
                    Debug.Log($"  Exploration: {currentDayState.ExplorationChallenge.Title}");
                if (currentDayState.DilemmaChallenge != null)
                    Debug.Log($"  Dilemma: {currentDayState.DilemmaChallenge.Title}");
                if (currentDayState.FamilyRequestChallenge != null)
                    Debug.Log($"  Family Request: {currentDayState.FamilyRequestChallenge.Title} (Target: {currentDayState.FamilyRequestTarget})");
            }

            OnDailyActionsReady?.Invoke(currentDayState);
        }

        /// <summary>
        /// Submit a single category's player input. Called from UI.
        /// </summary>
        public void SubmitAction(PlayerActionCategory category, string playerInput, List<string> selectedItems)
        {
            if (currentDayState == null)
            {
                Debug.LogError("[PlayerActionManager] No daily state prepared! Call PrepareDailyActions first.");
                return;
            }

            if (!currentDayState.IsCategoryActive(category))
            {
                Debug.LogWarning($"[PlayerActionManager] Category [{category}] is not active today.");
                return;
            }

            if (dayResults.ContainsKey(category))
            {
                Debug.LogWarning($"[PlayerActionManager] Category [{category}] already submitted.");
                return;
            }

            if (PlayerActionLLMBridge.Instance == null)
            {
                Debug.LogError("[PlayerActionManager] PlayerActionLLMBridge not found!");
                return;
            }

            // Store inputs in state
            switch (category)
            {
                case PlayerActionCategory.Exploration:
                    currentDayState.ExplorationInput = playerInput;
                    currentDayState.ExplorationItems = selectedItems ?? new List<string>();
                    break;
                case PlayerActionCategory.Dilemma:
                    currentDayState.DilemmaInput = playerInput;
                    currentDayState.DilemmaItems = selectedItems ?? new List<string>();
                    break;
                case PlayerActionCategory.FamilyRequest:
                    currentDayState.FamilyRequestInput = playerInput;
                    currentDayState.FamilyRequestItems = selectedItems ?? new List<string>();
                    break;
            }

            isProcessing = true;
            isProcessingDisplay = true;
            pendingResults++;

            var challenge = currentDayState.GetChallenge(category);
            int currentDay = currentDayState.Day;

            if (enableDebugLogs)
                Debug.Log($"[PlayerActionManager] Submitting [{category}]: \"{playerInput}\" with {selectedItems?.Count ?? 0} item(s)");

            PlayerActionLLMBridge.Instance.SendCategoryAction(
                category,
                challenge,
                playerInput,
                selectedItems,
                currentDay,
                totalDays,
                category == PlayerActionCategory.FamilyRequest ? currentDayState.FamilyRequestTarget : null,
                (result) => HandleCategoryResult(result)
            );
        }

        /// <summary>
        /// Submit all active categories at once. Convenience method.
        /// Syncs inspector input fields to currentDayState before submitting.
        /// </summary>
        public void SubmitAllActions()
        {
            Debug.Log($"[PlayerActionManager] SubmitAllActions called. currentDayState null? {currentDayState == null}");
            
            if (currentDayState == null) return;

            // Sync inspector fields to currentDayState (so inspector-entered values are submitted)
            if (currentDayState.ExplorationActive && !string.IsNullOrEmpty(explorationInput?.Trim()) && string.IsNullOrEmpty(currentDayState.ExplorationInput))
            {
                currentDayState.ExplorationInput = explorationInput.Trim();
            }
            if (currentDayState.DilemmaActive && !string.IsNullOrEmpty(dilemmaInput?.Trim()) && string.IsNullOrEmpty(currentDayState.DilemmaInput))
            {
                currentDayState.DilemmaInput = dilemmaInput.Trim();
            }
            if (currentDayState.FamilyRequestActive && !string.IsNullOrEmpty(familyRequestInput?.Trim()) && string.IsNullOrEmpty(currentDayState.FamilyRequestInput))
            {
                currentDayState.FamilyRequestInput = familyRequestInput.Trim();
            }

            var active = currentDayState.GetActiveCategories();
            Debug.Log($"[PlayerActionManager] Active categories: {active.Count} - {string.Join(", ", active)}");
            
            foreach (var cat in active)
            {
                string input = "";
                List<string> items = null;

                switch (cat)
                {
                    case PlayerActionCategory.Exploration:
                        input = currentDayState.ExplorationInput;
                        items = currentDayState.ExplorationItems;
                        explorationInput = ""; // Clear inspector field
                        break;
                    case PlayerActionCategory.Dilemma:
                        input = currentDayState.DilemmaInput;
                        items = currentDayState.DilemmaItems;
                        dilemmaInput = ""; // Clear inspector field
                        break;
                    case PlayerActionCategory.FamilyRequest:
                        input = currentDayState.FamilyRequestInput;
                        items = currentDayState.FamilyRequestItems;
                        familyRequestInput = ""; // Clear inspector field
                        break;
                }

                Debug.Log($"[PlayerActionManager] Category {cat}: input='{input}', alreadySubmitted={dayResults.ContainsKey(cat)}");
                
                if (!string.IsNullOrEmpty(input) && !dayResults.ContainsKey(cat))
                {
                    Debug.Log($"[PlayerActionManager] Submitting {cat} action...");
                    SubmitAction(cat, input, items);
                }
            }
        }

        /// <summary>
        /// Save a category's input without submitting to LLM.
        /// Stores the input in the daily state for later batch submission via SubmitAllActions().
        /// </summary>
        public void SaveAction(PlayerActionCategory category, string playerInput, List<string> selectedItems)
        {
            if (currentDayState == null)
            {
                Debug.LogWarning("[PlayerActionManager] No daily state prepared! Cannot save action.");
                return;
            }

            switch (category)
            {
                case PlayerActionCategory.Exploration:
                    currentDayState.ExplorationInput = playerInput;
                    currentDayState.ExplorationItems = selectedItems ?? new List<string>();
                    break;
                case PlayerActionCategory.Dilemma:
                    currentDayState.DilemmaInput = playerInput;
                    currentDayState.DilemmaItems = selectedItems ?? new List<string>();
                    break;
                case PlayerActionCategory.FamilyRequest:
                    currentDayState.FamilyRequestInput = playerInput;
                    currentDayState.FamilyRequestItems = selectedItems ?? new List<string>();
                    break;
            }

            // Update the inspector input fields
            SetInput(category, playerInput);

            if (enableDebugLogs)
                Debug.Log($"[PlayerActionManager] Saved [{category}]: \"{playerInput}\" with {selectedItems?.Count ?? 0} item(s). State updated.");

            OnCategorySaved?.Invoke(category);
        }

        // -------------------------------------------------------------------------
        // Result Handling
        // -------------------------------------------------------------------------
        private void HandleCategoryResult(PlayerActionResult result)
        {
            if (result == null) return;

            pendingResults--;
            dayResults[result.Category] = result;

            if (enableDebugLogs)
                Debug.Log($"[PlayerActionManager] [{result.Category}] result received: {(result.HasError ? "ERROR: " + result.Error : result.StoryEvent?.Title ?? "null")}");

            // Log to StoryLogSO
            if (!result.HasError && result.StoryEvent != null && storyLog != null)
            {
                storyLog.RecordEvent(
                    currentDayState.Day,
                    result.PlayerInput,
                    result.StoryEvent,
                    result.Category.ToString()
                );
            }

            // Execute effects via StorytellerManager pipeline (logs event, updates UI, executes effects)
            if (!result.HasError && result.StoryEvent != null)
            {
                // Try to get StorytellerManager (fallback to FindFirstObjectByType if singleton not initialized)
                var storyteller = StorytellerManager.Instance;
                if (storyteller == null)
                {
                    storyteller = FindFirstObjectByType<StorytellerManager>();
                    if (enableDebugLogs && storyteller != null)
                        Debug.Log("[PlayerActionManager] Found StorytellerManager via FindFirstObjectByType (singleton not yet initialized).");
                }

                if (storyteller != null)
                {
                    if (enableDebugLogs)
                        Debug.Log($"[PlayerActionManager] Forwarding event to StorytellerManager: {result.StoryEvent.Title}");
                    storyteller.ProcessEvent(result.StoryEvent);
                }
                else
                {
                    Debug.LogWarning("[PlayerActionManager] StorytellerManager not found in scene! Falling back to direct effect execution.");
                    // Fallback: execute effects directly if storyteller is missing
                    if (LLMEffectExecutor.Instance != null && result.StoryEvent.Effects != null)
                    {
                        LLMEffectExecutor.Instance.ExecuteEffects(result.StoryEvent.Effects);
                    }
                }

                // Consume used items
                ConsumeItems(result.ItemsUsed);
            }

            // Fire per-category event
            OnCategoryResultReceived?.Invoke(result);

            // Check if all done
            if (pendingResults <= 0)
            {
                isProcessing = false;
                isProcessingDisplay = false;

                if (enableDebugLogs)
                    Debug.Log($"[PlayerActionManager] All actions complete for Day {currentDayState.Day}. Total results: {dayResults.Count}");

                OnAllActionsComplete?.Invoke();
            }
        }

        // -------------------------------------------------------------------------
        // Item Consumption
        // -------------------------------------------------------------------------
        private void ConsumeItems(List<string> itemIds)
        {
            if (itemIds == null || itemIds.Count == 0) return;
            if (InventoryManager.Instance == null) return;

            foreach (var itemId in itemIds)
            {
                bool removed = InventoryManager.Instance.RemoveItem(itemId, 1);
                if (enableDebugLogs && removed)
                    Debug.Log($"[PlayerActionManager] Consumed item: {itemId}");
            }
        }

        // -------------------------------------------------------------------------
        // Availability Logic
        // -------------------------------------------------------------------------
        private bool ShouldActivateFamilyRequest()
        {
            // Random chance AND at least one family member has an issue
            if (UnityEngine.Random.value > familyRequestChance)
                return false;

            if (FamilyManager.Instance == null)
                return false;

            var family = FamilyManager.Instance.FamilyMembers;
            if (family == null || family.Count == 0)
                return false;

            // Check if any family member has an issue worth requesting help for
            foreach (var member in family)
            {
                if (!member.IsAlive) continue;
                if (member.IsSick || member.IsInjured || member.IsCritical || member.Sanity < 30f)
                    return true;
            }

            return false;
        }

        private string PickFamilyRequestTarget()
        {
            if (FamilyManager.Instance == null) return null;

            var family = FamilyManager.Instance.FamilyMembers;
            if (family == null || family.Count == 0) return null;

            // Prioritize members with issues
            var needyMembers = new List<CharacterData>();
            foreach (var member in family)
            {
                if (!member.IsAlive) continue;
                if (member.IsSick || member.IsInjured || member.IsCritical || member.Sanity < 30f)
                    needyMembers.Add(member);
            }

            if (needyMembers.Count > 0)
                return needyMembers[UnityEngine.Random.Range(0, needyMembers.Count)].Name;

            // Fallback to any alive member
            var alive = family.FindAll(m => m.IsAlive);
            if (alive.Count > 0)
                return alive[UnityEngine.Random.Range(0, alive.Count)].Name;

            return null;
        }

        // -------------------------------------------------------------------------
        // State Display Sync
        // -------------------------------------------------------------------------
        private void RefreshStateDisplay()
        {
            if (currentDayState != null)
            {
                explorationActive = currentDayState.ExplorationActive;
                dilemmaActive = currentDayState.DilemmaActive;
                familyRequestActive = currentDayState.FamilyRequestActive;
                familyRequestTargetDisplay = currentDayState.FamilyRequestTarget ?? "";
            }
            else
            {
                explorationActive = false;
                dilemmaActive = false;
                familyRequestActive = false;
                familyRequestTargetDisplay = "";
            }
            isProcessingDisplay = isProcessing;
        }

        // -------------------------------------------------------------------------
        // Player Input (editable in Inspector, synced at runtime)
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Player Inputs")]
        [InfoBox("Type your responses here or in the game UI. These fields sync with the category panels at runtime.")]
        [LabelText("Exploration Input")]
        [MultiLineProperty(3)]
        #endif
        [Header("Player Inputs")]
        [TextArea(2, 5)]
        [SerializeField] private string explorationInput = "";

        #if ODIN_INSPECTOR
        [LabelText("Dilemma Input")]
        [MultiLineProperty(3)]
        #endif
        [TextArea(2, 5)]
        [SerializeField] private string dilemmaInput = "";

        #if ODIN_INSPECTOR
        [LabelText("Family Request Input")]
        [MultiLineProperty(3)]
        #endif
        [TextArea(2, 5)]
        [SerializeField] private string familyRequestInput = "";

        // -------------------------------------------------------------------------
        // Input Sync (Inspector ↔ CategoryPanels)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Get the inspector input for a category.
        /// </summary>
        public string GetInput(PlayerActionCategory category)
        {
            switch (category)
            {
                case PlayerActionCategory.Exploration: return explorationInput ?? "";
                case PlayerActionCategory.Dilemma: return dilemmaInput ?? "";
                case PlayerActionCategory.FamilyRequest: return familyRequestInput ?? "";
                default: return "";
            }
        }

        /// <summary>
        /// Set the inspector input for a category (called by UI panels to sync back).
        /// Also syncs to currentDayState so SubmitAllActions() can find the input.
        /// </summary>
        public void SetInput(PlayerActionCategory category, string value)
        {
            // Update inspector fields
            switch (category)
            {
                case PlayerActionCategory.Exploration: explorationInput = value; break;
                case PlayerActionCategory.Dilemma: dilemmaInput = value; break;
                case PlayerActionCategory.FamilyRequest: familyRequestInput = value; break;
            }
            
            // Also sync to currentDayState so SubmitAllActions() can find the input
            if (currentDayState != null)
            {
                switch (category)
                {
                    case PlayerActionCategory.Exploration:
                        currentDayState.ExplorationInput = value;
                        break;
                    case PlayerActionCategory.Dilemma:
                        currentDayState.DilemmaInput = value;
                        break;
                    case PlayerActionCategory.FamilyRequest:
                        currentDayState.FamilyRequestInput = value;
                        break;
                }
            }
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug Actions")]
        [Button("Prepare Day Actions (Current Day)", ButtonSizes.Large)]
        [GUIColor(0, 1, 0)]
        private void Debug_PrepareDayActions()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode to test."); return; }

            int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;
            PrepareDailyActions(day);
        }

        [Button("Force Prepare All Active", ButtonSizes.Medium)]
        private void Debug_ForceAllActive()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode to test."); return; }

            int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;

            // Force all categories active for testing
            dayResults.Clear();
            pendingResults = 0;
            isProcessing = false;

            currentDayState = new DailyActionState
            {
                Day = day,
                ExplorationActive = true,
                DilemmaActive = true,
                FamilyRequestActive = true
            };

            currentDayState.ExplorationChallenge = challengePool != null ? challengePool.GetRandomExploration() : null;
            currentDayState.DilemmaChallenge = challengePool != null ? challengePool.GetRandomDilemma() : null;
            currentDayState.FamilyRequestChallenge = challengePool != null ? challengePool.GetRandomFamilyRequest() : null;
            currentDayState.FamilyRequestTarget = PickFamilyRequestTarget();

            RefreshStateDisplay();
            Debug.Log($"[Debug] Forced all categories active for Day {day}");
            OnDailyActionsReady?.Invoke(currentDayState);
        }

        [Button("Submit Exploration", ButtonSizes.Medium)]
        [EnableIf("explorationActive")]
        private void Debug_TestSubmitExploration()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode to test."); return; }
            if (currentDayState == null) { Debug.LogWarning("Prepare day actions first!"); return; }
            if (!currentDayState.ExplorationActive) { Debug.LogWarning("Exploration not active today!"); return; }
            if (string.IsNullOrEmpty(explorationInput?.Trim())) { Debug.LogWarning("Type something in the Exploration Input field first!"); return; }
            SubmitAction(PlayerActionCategory.Exploration, explorationInput.Trim(), null);
        }

        [Button("Submit Dilemma", ButtonSizes.Medium)]
        [EnableIf("dilemmaActive")]
        private void Debug_TestSubmitDilemma()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode to test."); return; }
            if (currentDayState == null) { Debug.LogWarning("Prepare day actions first!"); return; }
            if (!currentDayState.DilemmaActive) { Debug.LogWarning("Dilemma not active today!"); return; }
            if (string.IsNullOrEmpty(dilemmaInput?.Trim())) { Debug.LogWarning("Type something in the Dilemma Input field first!"); return; }
            SubmitAction(PlayerActionCategory.Dilemma, dilemmaInput.Trim(), null);
        }

        [Button("Submit Family Request", ButtonSizes.Medium)]
        [EnableIf("familyRequestActive")]
        private void Debug_TestSubmitFamily()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode to test."); return; }
            if (currentDayState == null) { Debug.LogWarning("Prepare day actions first!"); return; }
            if (!currentDayState.FamilyRequestActive) { Debug.LogWarning("Family request not active today!"); return; }
            if (string.IsNullOrEmpty(familyRequestInput?.Trim())) { Debug.LogWarning("Type something in the Family Request Input field first!"); return; }
            SubmitAction(PlayerActionCategory.FamilyRequest, familyRequestInput.Trim(), new List<string>());
        }

        [Button("Submit All Active Inputs", ButtonSizes.Large)]
        [GUIColor(0.2f, 0.6f, 1f)]
        private void Debug_SubmitAll()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode to test."); return; }
            if (currentDayState == null) { Debug.LogWarning("Prepare day actions first!"); return; }

            // Only submit active categories that have input
            if (currentDayState.ExplorationActive && !string.IsNullOrEmpty(explorationInput?.Trim()))
            {
                currentDayState.ExplorationInput = explorationInput.Trim();
            }
            if (currentDayState.DilemmaActive && !string.IsNullOrEmpty(dilemmaInput?.Trim()))
            {
                currentDayState.DilemmaInput = dilemmaInput.Trim();
            }
            if (currentDayState.FamilyRequestActive && !string.IsNullOrEmpty(familyRequestInput?.Trim()))
            {
                currentDayState.FamilyRequestInput = familyRequestInput.Trim();
            }

            SubmitAllActions();
        }

        [Button("Log Current State", ButtonSizes.Medium)]
        private void Debug_LogState()
        {
            if (currentDayState == null) { Debug.Log("[Debug] No daily state."); return; }
            Debug.Log($"[Debug] Day {currentDayState.Day} | Active: {string.Join(", ", currentDayState.GetActiveCategories())} | Results: {dayResults.Count} | Pending: {pendingResults} | Processing: {isProcessing}");
            foreach (var kvp in dayResults)
            {
                Debug.Log($"  [{kvp.Key}] {(kvp.Value.HasError ? "ERROR" : kvp.Value.StoryEvent?.Title ?? "null")}");
            }
        }
        #endif

        // -------------------------------------------------------------------------
        // Auto Setup
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Setup")]
        [Button("Auto Setup", ButtonSizes.Large)]
        [GUIColor(0.2f, 0.8f, 0.2f)]
        #endif
        public void AutoSetup()
        {
            gameObject.name = "[PlayerActionManager]";

            // Find challenge pool
            if (challengePool == null)
            {
                challengePool = Resources.Load<PlayerActionChallengePoolSO>("PlayerActionChallengePool");
                if (challengePool == null)
                {
                    #if UNITY_EDITOR
                    string[] guids = UnityEditor.AssetDatabase.FindAssets("t:PlayerActionChallengePoolSO");
                    if (guids.Length > 0)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                        challengePool = UnityEditor.AssetDatabase.LoadAssetAtPath<PlayerActionChallengePoolSO>(path);
                    }
                    #endif
                }
                if (challengePool == null)
                    Debug.LogWarning("[PlayerActionManager] ChallengePool not found! Create one via Create > TheBunkerGames > Player Action Challenge Pool.");
            }

            // Find story log
            if (storyLog == null)
            {
                #if UNITY_EDITOR
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:StoryLogSO");
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    storyLog = UnityEditor.AssetDatabase.LoadAssetAtPath<StoryLogSO>(path);
                }
                #endif
                if (storyLog == null)
                    Debug.LogWarning("[PlayerActionManager] StoryLogSO not found!");
            }

            // Ensure LLM bridge exists
            if (PlayerActionLLMBridge.Instance == null)
            {
                var bridge = FindFirstObjectByType<PlayerActionLLMBridge>();
                if (bridge == null)
                {
                    var bridgeGO = new GameObject("[PlayerActionLLMBridge]");
                    bridgeGO.AddComponent<PlayerActionLLMBridge>();
                    Debug.Log("[PlayerActionManager] Created [PlayerActionLLMBridge] GameObject.");
                }
            }

            Debug.Log("[PlayerActionManager] Auto Setup Complete.");
        }
    }
}
