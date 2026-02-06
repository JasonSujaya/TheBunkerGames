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
    public class CityExplorationController : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static CityExplorationController Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------
        public static event Action<ExplorationResult> OnExplorationComplete;
        public static event Action<CharacterData, ExplorationLocation> OnCharacterSentOut;
        public static event Action OnExplorationPhaseComplete;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Active Expeditions")]
        [ReadOnly]
        #endif
        [SerializeField] private List<Expedition> activeExpeditions = new List<Expedition>();

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
                var result = ResolveExpedition(expedition);
                expedition.IsComplete = true;
                expedition.Result = result;

                // Apply results to character
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
        // Expedition Resolution
        // -------------------------------------------------------------------------
        private ExplorationResult ResolveExpedition(Expedition expedition)
        {
            var result = new ExplorationResult
            {
                ExplorerName = expedition.ExplorerName,
                LocationName = expedition.Location.LocationName,
                FoundItems = new List<ResourceGrantData>(),
                NarrativeLog = ""
            };

            float riskFactor = GetRiskFactor(expedition.Location.Risk);

            // Determine if injured
            result.IsInjured = UnityEngine.Random.value < riskFactor * 0.5f;
            result.HealthChange = result.IsInjured ? -UnityEngine.Random.Range(10f, 30f) : 0f;
            result.SanityChange = -UnityEngine.Random.Range(5f, 15f) * riskFactor;

            // Generate loot based on risk (higher risk = better rewards)
            int lootCount = Mathf.FloorToInt(UnityEngine.Random.Range(0f, 3f) * riskFactor + 1);
            for (int i = 0; i < lootCount; i++)
            {
                string itemId = GenerateRandomLootId(expedition.Location.Risk);
                result.FoundItems.Add(new ResourceGrantData(itemId, 1));
            }

            result.NarrativeLog = $"{expedition.ExplorerName} ventured into {expedition.Location.LocationName}. ";
            if (result.IsInjured)
            {
                result.NarrativeLog += "They returned battered and bruised. ";
            }
            result.NarrativeLog += $"They found {result.FoundItems.Count} item(s).";

            return result;
        }

        private float GetRiskFactor(ExplorationRisk risk)
        {
            switch (risk)
            {
                case ExplorationRisk.Low: return 0.3f;
                case ExplorationRisk.Medium: return 0.6f;
                case ExplorationRisk.High: return 0.85f;
                case ExplorationRisk.Deadly: return 1.0f;
                default: return 0.5f;
            }
        }

        private string GenerateRandomLootId(ExplorationRisk risk)
        {
            // Placeholder IDs — in the real game, A.N.G.E.L./Neocortex generates these dynamically
            string[] lowLoot = { "scrap_metal", "dirty_rag", "empty_bottle" };
            string[] medLoot = { "canned_food", "bandages", "water_bottle" };
            string[] highLoot = { "medicine", "tools", "radio_parts" };
            string[] deadlyLoot = { "rare_medicine", "weapon_parts", "power_cell" };

            string[] pool;
            switch (risk)
            {
                case ExplorationRisk.Low: pool = lowLoot; break;
                case ExplorationRisk.Medium: pool = medLoot; break;
                case ExplorationRisk.High: pool = highLoot; break;
                case ExplorationRisk.Deadly: pool = deadlyLoot; break;
                default: pool = lowLoot; break;
            }

            return pool[UnityEngine.Random.Range(0, pool.Length)];
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug Controls")]
        [Button("Send First CharacterData (Medium Risk)", ButtonSizes.Medium)]
        [GUIColor(0.5f, 1f, 0.5f)]
        private void Debug_SendFirstCharacter()
        {
            if (!Application.isPlaying) return;
            var family = FamilyManager.Instance;
            if (family == null || family.FamilyMembers.Count == 0) return;

            var testLocation = new ExplorationLocation
            {
                LocationName = "Abandoned Store",
                Risk = ExplorationRisk.Medium,
                Description = "A half-collapsed convenience store."
            };
            SendCharacterToExplore(family.FamilyMembers[0], testLocation);
        }

        [Button("Resolve All Expeditions", ButtonSizes.Medium)]
        [GUIColor(1f, 0.7f, 0.5f)]
        private void Debug_ResolveAll()
        {
            if (Application.isPlaying) ResolveExpeditions();
        }

        [Button("Complete Phase", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.8f, 1f)]
        private void Debug_CompletePhase()
        {
            if (Application.isPlaying) CompleteExplorationPhase();
        }
        #endif
    }
}
