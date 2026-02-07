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
        [Title("Debug")]
        [ReadOnly]
        #endif
        [Header("State")]
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
        /// </summary>
        public void SubmitAllActions()
        {
            if (currentDayState == null) return;

            var active = currentDayState.GetActiveCategories();
            foreach (var cat in active)
            {
                string input = "";
                List<string> items = null;

                switch (cat)
                {
                    case PlayerActionCategory.Exploration:
                        input = currentDayState.ExplorationInput;
                        items = currentDayState.ExplorationItems;
                        break;
                    case PlayerActionCategory.Dilemma:
                        input = currentDayState.DilemmaInput;
                        items = currentDayState.DilemmaItems;
                        break;
                    case PlayerActionCategory.FamilyRequest:
                        input = currentDayState.FamilyRequestInput;
                        items = currentDayState.FamilyRequestItems;
                        break;
                }

                if (!string.IsNullOrEmpty(input) && !dayResults.ContainsKey(cat))
                {
                    SubmitAction(cat, input, items);
                }
            }
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

            // Execute effects via StorytellerManager pipeline
            if (!result.HasError && result.StoryEvent != null)
            {
                // Execute immediate effects
                if (LLMEffectExecutor.Instance != null && result.StoryEvent.Effects != null)
                {
                    LLMEffectExecutor.Instance.ExecuteEffects(result.StoryEvent.Effects);
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

            Debug.Log($"[Debug] Forced all categories active for Day {day}");
            OnDailyActionsReady?.Invoke(currentDayState);
        }

        [Button("Test Submit Exploration", ButtonSizes.Medium)]
        private void Debug_TestSubmitExploration()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode to test."); return; }
            if (currentDayState == null) { Debug.LogWarning("Prepare day actions first!"); return; }
            SubmitAction(PlayerActionCategory.Exploration, "I'll carefully search the room for supplies and check behind the furniture.", null);
        }

        [Button("Test Submit Dilemma", ButtonSizes.Medium)]
        private void Debug_TestSubmitDilemma()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode to test."); return; }
            if (currentDayState == null) { Debug.LogWarning("Prepare day actions first!"); return; }
            if (!currentDayState.DilemmaActive) { Debug.LogWarning("Dilemma not active today!"); return; }
            SubmitAction(PlayerActionCategory.Dilemma, "We should share the water equally among everyone.", null);
        }

        [Button("Test Submit Family", ButtonSizes.Medium)]
        private void Debug_TestSubmitFamily()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode to test."); return; }
            if (currentDayState == null) { Debug.LogWarning("Prepare day actions first!"); return; }
            if (!currentDayState.FamilyRequestActive) { Debug.LogWarning("Family request not active today!"); return; }
            SubmitAction(PlayerActionCategory.FamilyRequest, "I'll give them medicine and comfort them.", new List<string>());
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
