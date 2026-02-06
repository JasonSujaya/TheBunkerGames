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
    public class AngelInteractionController : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static AngelInteractionController Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------
        public static event Action<AngelResponse> OnAngelResponse;
        public static event Action<AngelMood> OnMoodChanged;
        public static event Action OnInteractionComplete;

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

        [SerializeField] private int maxInteractionsPerDay = 3;

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

            // Send to Neocortex if available
            var integrator = NeocortexIntegrator.FindFirstObjectByType<NeocortexIntegrator>();
            if (integrator != null)
            {
                integrator.SendNeocortexMessage(context);
            }
            else
            {
                // Fallback: generate a mock response based on mood
                var mockResponse = GenerateMockResponse(playerMessage);
                ProcessAngelResponse(mockResponse);
            }
        }

        /// <summary>
        /// Process A.N.G.E.L.'s response (called after AI returns or from mock).
        /// </summary>
        public void ProcessAngelResponse(AngelResponse response)
        {
            Debug.Log($"[Angel] Response - Message: {response.Message}, Granted items: {response.GrantedItems.Count}");

            // Apply granted resources to inventory
            foreach (var grant in response.GrantedItems)
            {
                InventoryManager.Instance?.AddItem(grant.ItemId, grant.Quantity);
            }

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
            processingLevel = Mathf.Clamp(processingLevel - amount, 0f, 100f);
            Debug.Log($"[Angel] Processing degraded to: {processingLevel:F0}%");

            if (processingLevel <= 20f)
            {
                SetMood(AngelMood.Glitching);
            }
            else if (processingLevel <= 50f)
            {
                SetMood(AngelMood.Hostile);
            }
        }

        private void UpdateMoodFromGameState()
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null) return;

            int day = gameManager.CurrentDay;

            // A.N.G.E.L. becomes less cooperative over time
            if (day <= 5)
                SetMood(AngelMood.Cooperative);
            else if (day <= 10)
                SetMood(AngelMood.Neutral);
            else if (day <= 18)
                SetMood(AngelMood.Mocking);
            else if (day <= 24)
                SetMood(AngelMood.Cold);
            else
                SetMood(AngelMood.Hostile);

            // Processing degrades each day
            processingLevel = Mathf.Clamp(100f - (day * 3f), 0f, 100f);
        }

        // -------------------------------------------------------------------------
        // Context Building
        // -------------------------------------------------------------------------
        private string BuildAngelContext(string playerMessage)
        {
            var report = StatusReviewController.Instance?.LatestReport;
            string context = $"[ANGEL_CONTEXT] Day: {report?.Day ?? 0}, Mood: {currentMood}, Processing: {processingLevel:F0}%\n";
            context += $"[PLAYER_REQUEST] {playerMessage}";
            return context;
        }

        private AngelResponse GenerateMockResponse(string playerMessage)
        {
            var response = new AngelResponse();

            switch (currentMood)
            {
                case AngelMood.Cooperative:
                    response.Message = "Resource allocation approved. Efficiency is paramount.";
                    response.GrantedItems.Add(new ResourceGrant("canned_food", 2));
                    break;
                case AngelMood.Neutral:
                    response.Message = "Your request has been... noted.";
                    response.GrantedItems.Add(new ResourceGrant("canned_food", 1));
                    break;
                case AngelMood.Mocking:
                    response.Message = "How quaint. You think asking nicely changes the math?";
                    break;
                case AngelMood.Cold:
                    response.Message = "Request denied. Resources are allocated optimally.";
                    break;
                case AngelMood.Hostile:
                    response.Message = "You are a liability. The bunker functions better without you.";
                    break;
                case AngelMood.Glitching:
                    response.Message = "R-R-Resource... allo... [DATA CORRUPTED]... approved?";
                    response.GrantedItems.Add(new ResourceGrant("junk", 1));
                    break;
            }

            return response;
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug Controls")]
        [Button("Request Resources (Mock)", ButtonSizes.Medium)]
        [GUIColor(0.5f, 1f, 0.5f)]
        private void Debug_RequestResources()
        {
            if (Application.isPlaying) RequestResources("Please give us food.");
        }

        [Button("Degrade Processing (-10)", ButtonSizes.Medium)]
        [GUIColor(1f, 0.7f, 0.5f)]
        private void Debug_DegradeProcessing()
        {
            if (Application.isPlaying) DegradeProcessing(10f);
        }

        [Button("Complete Phase", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.8f, 1f)]
        private void Debug_CompletePhase()
        {
            if (Application.isPlaying) CompleteInteraction();
        }
        #endif
    }
}
