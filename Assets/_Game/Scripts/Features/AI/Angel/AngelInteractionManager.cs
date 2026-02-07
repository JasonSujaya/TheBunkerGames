using UnityEngine;
using System;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Controls the A.N.G.E.L. Interaction phase (Phase 2 of the Core Loop).
    /// Players argue with A.N.G.E.L. for resources. Her mood and "Processing" level
    /// determine cooperation. Sends context to Neocortex and processes AI responses.
    /// </summary>
    public class AngelInteractionManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static AngelInteractionManager Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------
        public static event Action<AngelResponseData> OnAngelResponse;
        public static event Action<AngelMood> OnMoodChanged;
        public static event Action OnInteractionComplete;

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Settings")]
        #endif
        [SerializeField] private AngelResponsesSO responsesData;
        [SerializeField] private int maxInteractionsPerDay = 3;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("A.N.G.E.L. State")]
        [ReadOnly]
        #endif
        [SerializeField] private AngelMood currentMood = AngelMood.Neutral;

        #if ODIN_INSPECTOR
        [ReadOnly]
        [ProgressBar(0, 100)]
        #endif
        [SerializeField] private float processingLevel = 100f;

        #if ODIN_INSPECTOR
        [ReadOnly]
        #endif
        [SerializeField] private int interactionsThisDay;

        // -------------------------------------------------------------------------
        // Logic Controller
        // -------------------------------------------------------------------------
        private AngelLogicController logicController;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public AngelMood CurrentMood => currentMood;
        public float ProcessingLevel => processingLevel;
        public bool CanInteract => interactionsThisDay < maxInteractionsPerDay;

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
            logicController = new AngelLogicController();
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
            if (newState == GameState.AngelInteraction)
            {
                BeginInteractionPhase();
            }
        }

        public void BeginInteractionPhase()
        {
            interactionsThisDay = 0;
            UpdateMoodFromGameState();
            Debug.Log($"[Angel] Interaction phase started. Mood: {currentMood}, Processing: {processingLevel:F0}%");
        }

        /// <summary>
        /// Request resources from A.N.G.E.L. The request is sent to the AI
        /// along with game context, and the response determines what you get.
        /// </summary>
        public void RequestResources(string playerMessage)
        {
            if (!CanInteract)
            {
                Debug.LogWarning("[Angel] No more interactions allowed today.");
                return;
            }

            interactionsThisDay++;

            // Build context for the AI
            string context = BuildAngelContext(playerMessage);

            // Send to LLMManager
            if (LLMManager.Instance != null)
            {
                LLMManager.Instance.QuickChat(
                    context,
                    onSuccess: (res) => {
                        var mockResponse = logicController.GenerateMockResponse(currentMood, responsesData);
                        mockResponse.Message = res; // Inject AI text
                        ProcessAngelResponse(mockResponse);
                    },
                    onError: (err) => {
                        Debug.LogError($"[Angel] AI Request failed: {err}");
                        var fallback = logicController.GenerateMockResponse(currentMood, responsesData);
                        ProcessAngelResponse(fallback);
                    }
                );
            }
            else
            {
                // Fallback: generate a mock response
                var mockResponse = logicController.GenerateMockResponse(currentMood, responsesData);
                ProcessAngelResponse(mockResponse);
            }
        }

        /// <summary>
        /// Process A.N.G.E.L.'s response (called after AI returns or from mock).
        /// </summary>
        public void ProcessAngelResponse(AngelResponseData response)
        {
            Debug.Log($"[Angel] Response - Message: {response.Message}, Granted items: {response.GrantedItems.Count}");

            // Apply granted resources to inventory
            foreach (var grant in response.GrantedItems)
            {
                InventoryManager.Instance?.AddItem(grant.ItemId, grant.Quantity);
            }

            // Trigger a minor glitch burst on response for juice
            UIGlitchController.Instance?.TriggerBurst(0.3f, 0.2f);
            OnAngelResponse?.Invoke(response);
        }

        public void CompleteInteraction()
        {
            Debug.Log("[Angel] Interaction phase complete. Moving to City Exploration.");
            OnInteractionComplete?.Invoke();
        }

        // -------------------------------------------------------------------------
        // Mood & Processing
        // -------------------------------------------------------------------------
        public void SetMood(AngelMood mood)
        {
            if (currentMood == mood) return;
            currentMood = mood;
            Debug.Log($"[Angel] Mood changed to: {mood}");
            OnMoodChanged?.Invoke(mood);
        }

        public void DegradeProcessing(float amount)
        {
            processingLevel = logicController.CalculateProcessingLevel(processingLevel, amount);
            Debug.Log($"[Angel] Processing degraded to: {processingLevel:F0}%");
            
            // Re-evaluate mood based on new processing level
            var day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 0;
            var newMood = logicController.DetermineMood(processingLevel, day);
            SetMood(newMood);
        }

        private void UpdateMoodFromGameState()
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null) return;

            int day = gameManager.CurrentDay;

            // Update processing based on day (simple linear decay for now, handled here as it's state initialization)
            processingLevel = Mathf.Clamp(100f - (day * 3f), 0f, 100f);

            // Determine mood using Controller
            var newMood = logicController.DetermineMood(processingLevel, day);
            SetMood(newMood);
        }

        // -------------------------------------------------------------------------
        // Context Building
        // -------------------------------------------------------------------------
        private string BuildAngelContext(string playerMessage)
        {
            var report = StatusReviewManager.Instance?.LatestReport;
            string context = $"[ANGEL_CONTEXT] Day: {report?.Day ?? 0}, Mood: {currentMood}, Processing: {processingLevel:F0}%\n";
            context += $"[PLAYER_REQUEST] {playerMessage}";
            return context;
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug Controls")]
        [Button("Request Resources (Mock)", ButtonSizes.Medium)]
        private void Debug_RequestResources()
        {
            if (Application.isPlaying) RequestResources("Please give us food.");
        }

        [Button("Degrade Processing (-10)", ButtonSizes.Medium)]
        private void Debug_DegradeProcessing()
        {
            if (Application.isPlaying) DegradeProcessing(10f);
        }

        [Button("Complete Phase", ButtonSizes.Medium)]
        private void Debug_CompletePhase()
        {
            if (Application.isPlaying) CompleteInteraction();
        }

        [Title("Auto Setup")]
        [Button("Auto Setup Dependencies", ButtonSizes.Large)]
        private void AutoSetupDependencies()
        {
            #if UNITY_EDITOR
            // Ensure Tester exists
            var testerType = System.Type.GetType("TheBunkerGames.Tests.AngelInteractionTester");
            if (testerType != null && GetComponent(testerType) == null)
            {
                gameObject.AddComponent(testerType);
                Debug.Log("[AngelInteractionManager] Added AngelInteractionTester.");
            }
            else if (testerType == null)
            {
                Debug.LogWarning("[AngelInteractionManager] Could not find AngelInteractionTester type. Ensure it is in TheBunkerGames.Tests namespace.");
            }
            #endif
        }
        #endif
    }
}
