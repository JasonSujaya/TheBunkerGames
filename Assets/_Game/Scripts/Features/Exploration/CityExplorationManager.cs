using UnityEngine;
using System;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Controls the City Exploration phase (Phase 3 of the Core Loop).
    /// Players send family members into the wasteland to scavenge for tools and food.
    /// Risk/reward system with location-based outcomes.
    /// </summary>
    public class CityExplorationManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static CityExplorationManager Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------
        public static event Action<ExplorationResult> OnExplorationComplete;
        public static event Action<CharacterData, ExplorationLocation> OnCharacterSentOut;
        public static event Action OnExplorationPhaseComplete;

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Settings")]
        #endif
        [SerializeField] private LootTableSO lootTable;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Active Expeditions")]
        [ReadOnly]
        #endif
        [SerializeField] private List<Expedition> activeExpeditions = new List<Expedition>();

        // -------------------------------------------------------------------------
        // Logic Controller
        // -------------------------------------------------------------------------
        private ExplorationRulesController rulesController;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public List<Expedition> ActiveExpeditions => activeExpeditions;

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

            // Initialize Logic Controller
            rulesController = new ExplorationRulesController();
        }

        private void OnEnable()
        {
            GameManager.OnStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            GameManager.OnStateChanged -= HandleStateChanged;
        }

        // -------------------------------------------------------------------------
        // Phase Logic
        // -------------------------------------------------------------------------
        private void HandleStateChanged(GameState newState)
        {
            if (newState == GameState.CityExploration)
            {
                BeginExplorationPhase();
            }
        }

        public void BeginExplorationPhase()
        {
            activeExpeditions.Clear();
            Debug.Log("[CityExploration] Exploration phase started. Choose who to send out.");
        }

        /// <summary>
        /// Send a character to explore a location. Returns false if character isn't available.
        /// </summary>
        public bool SendCharacterToExplore(CharacterData character, ExplorationLocation location)
        {
            if (character == null || location == null)
            {
                Debug.LogWarning("[CityExploration] Invalid character or location.");
                return false;
            }

            if (!character.IsAvailableForExploration)
            {
                Debug.LogWarning($"[CityExploration] {character.Name} is not available for exploration.");
                return false;
            }

            character.IsExploring = true;

            var expedition = new Expedition
            {
                ExplorerName = character.Name,
                Location = location,
                IsComplete = false
            };
            activeExpeditions.Add(expedition);

            Debug.Log($"[CityExploration] {character.Name} sent to {location.LocationName} (Risk: {location.Risk})");
            OnCharacterSentOut?.Invoke(character, location);
            return true;
        }

        /// <summary>
        /// Resolve all active expeditions and generate results.
        /// In the real game, Neocortex generates unique narrative outcomes.
        /// </summary>
        public void ResolveExpeditions()
        {
            foreach (var expedition in activeExpeditions)
            {
                // Use the Controller to handle the math/logic
                var result = rulesController.ResolveExpedition(expedition, lootTable);
                
                expedition.IsComplete = true;
                expedition.Result = result;

                // Apply results to character (State mutation remains in Manager for now, or could act on Managers)
                var character = FamilyManager.Instance?.GetCharacter(expedition.ExplorerName);
                if (character != null)
                {
                    character.IsExploring = false;
                    character.ModifyHealth(result.HealthChange);
                    character.ModifySanity(result.SanityChange);

                    if (result.IsInjured)
                    {
                        character.IsInjured = true;
                    }

                    // Add found items to inventory
                    foreach (var loot in result.FoundItems)
                    {
                        InventoryManager.Instance?.AddItem(loot.ItemId, loot.Quantity);
                    }
                }

                Debug.Log($"[CityExploration] {expedition.ExplorerName} returned from {expedition.Location.LocationName}. " +
                         $"Found {result.FoundItems.Count} items. Injured: {result.IsInjured}");
                OnExplorationComplete?.Invoke(result);
            }
        }

        public void CompleteExplorationPhase()
        {
            ResolveExpeditions();
            Debug.Log("[CityExploration] Exploration phase complete. Moving to Daily Choice.");
            OnExplorationPhaseComplete?.Invoke();
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug Controls")]
        [Button("Send First CharacterData (Medium Risk)", ButtonSizes.Medium)]
        private void Debug_SendFirstCharacter()
        {
            if (!Application.isPlaying) return;
            var family = FamilyManager.Instance;
            if (family == null || family.FamilyMembers.Count == 0) return;

            var testLocation = new ExplorationLocation
            {
                LocationName = "Abandoned Store",
                Risk = ExplorationRisk.Medium,
                Description = "A half-collapsed convenience store.",
                IsAvailable = true
            };
            SendCharacterToExplore(family.FamilyMembers[0], testLocation);
        }

        [Button("Resolve All Expeditions", ButtonSizes.Medium)]
        private void Debug_ResolveAll()
        {
            if (Application.isPlaying) ResolveExpeditions();
        }

        [Button("Complete Phase", ButtonSizes.Medium)]
        private void Debug_CompletePhase()
        {
            if (Application.isPlaying) CompleteExplorationPhase();
        }

        [Title("Auto Setup")]
        [Button("Auto Setup Dependencies", ButtonSizes.Large)]
        private void AutoSetupDependencies()
        {
            #if UNITY_EDITOR
            // Ensure Tester exists
            var testerType = System.Type.GetType("TheBunkerGames.Tests.CityExplorationManagerTester");
            if (testerType != null && GetComponent(testerType) == null)
            {
                gameObject.AddComponent(testerType);
                Debug.Log("[CityExplorationManager] Added ExplorationTester.");
            }
            else if (testerType == null)
            {
                Debug.LogWarning("[CityExplorationManager] Could not find ExplorationTester type. Ensure it is in TheBunkerGames.Tests namespace.");
            }
            #endif
        }
        #endif
    }
}
