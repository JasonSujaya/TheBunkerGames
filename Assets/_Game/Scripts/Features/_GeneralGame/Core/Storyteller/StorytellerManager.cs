using UnityEngine;
using System;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Manages story events using LLM-generated JSON data.
    /// Simplified to work directly with LLMStoryEventData instead of ScriptableObjects.
    /// </summary>
    public class StorytellerManager : MonoBehaviour
    {
        public static StorytellerManager Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("References")]
        #endif
        [Header("UI Reference")]
        [SerializeField] private StorytellerUI ui;

        #if ODIN_INSPECTOR
        [Title("Current Event")]
        [ReadOnly]
        #endif
        [SerializeField] private string currentEventTitle;
        [SerializeField, TextArea(3, 5)] private string currentEventDescription;

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------
        public static event Action<LLMStoryEventData> OnStoryEventReceived;
        public static event Action<LLMStoryChoice> OnChoiceMade;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------
        public LLMStoryEventData CurrentEvent { get; private set; }

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

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        /// <summary>
        /// Process a story event from JSON string (from LLM response).
        /// </summary>
        public void ProcessEventFromJson(string json)
        {
            var storyEvent = LLMStoryEventData.FromJson(json);
            if (storyEvent == null)
            {
                Debug.LogError("[StorytellerManager] Failed to parse story event JSON");
                return;
            }
            ProcessEvent(storyEvent);
        }

        /// <summary>
        /// Process a story event directly.
        /// </summary>
        public void ProcessEvent(LLMStoryEventData storyEvent)
        {
            if (storyEvent == null) return;

            CurrentEvent = storyEvent;
            currentEventTitle = storyEvent.Title;
            currentEventDescription = storyEvent.Description;

            Debug.Log($"[Storyteller] Processing Event: {storyEvent.Title}");
            OnStoryEventReceived?.Invoke(storyEvent);

            // Update UI
            if (ui != null)
            {
                // Ensure the UI GameObject is enabled
                if (!ui.gameObject.activeInHierarchy)
                    ui.gameObject.SetActive(true);
                    
                ui.ShowEvent(storyEvent);
            }

            // Execute immediate effects
            ExecuteEffects(storyEvent.Effects);
        }

        /// <summary>
        /// Player selects a choice.
        /// </summary>
        public void MakeChoice(int choiceIndex)
        {
            if (CurrentEvent == null || CurrentEvent.Choices == null)
            {
                Debug.LogWarning("[Storyteller] No current event or choices available");
                return;
            }

            if (choiceIndex < 0 || choiceIndex >= CurrentEvent.Choices.Count)
            {
                Debug.LogWarning($"[Storyteller] Invalid choice index: {choiceIndex}");
                return;
            }

            var choice = CurrentEvent.Choices[choiceIndex];
            Debug.Log($"[Storyteller] Choice made: {choice.Text}");
            
            OnChoiceMade?.Invoke(choice);
            ExecuteEffects(choice.Effects);
        }

        // -------------------------------------------------------------------------
        // Private Methods
        // -------------------------------------------------------------------------
        private void ExecuteEffects(System.Collections.Generic.List<LLMStoryEffectData> effects)
        {
            if (effects == null || effects.Count == 0) return;

            if (LLMEffectExecutor.Instance == null)
            {
                Debug.LogWarning("[Storyteller] LLMEffectExecutor not found - effects not executed");
                return;
            }

            LLMEffectExecutor.Instance.ExecuteEffects(effects);
        }

        // -------------------------------------------------------------------------
        // Debug
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug")]
        [Button("Test Sample Event", ButtonSizes.Large)]
        [GUIColor(0, 1, 0)]
        private void Debug_TestSampleEvent()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode to test.");
                return;
            }

            var sampleEvent = LLMStoryEventData.CreateSample();
            Debug.Log($"[Storyteller] Testing sample event:\n{sampleEvent.ToJson(true)}");
            ProcessEvent(sampleEvent);
        }

        [Button("Clear Current Event")]
        private void Debug_ClearEvent()
        {
            CurrentEvent = null;
            currentEventTitle = "";
            currentEventDescription = "";
            if (ui != null) ui.Hide();
        }
        #endif
    }
}

