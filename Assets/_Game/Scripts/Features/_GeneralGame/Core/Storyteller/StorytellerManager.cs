using UnityEngine;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    public class StorytellerManager : MonoBehaviour
    {
        public static StorytellerManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private StoryScenarioSO currentScenario;

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

        private void Start()
        {
            // Subscribe to Game Events
            if (GameManager.Instance != null)
            {
                GameManager.OnDayStart += HandleDayStart;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.OnDayStart -= HandleDayStart;
            }
        }

        // -------------------------------------------------------------------------
        // Event Handling
        // -------------------------------------------------------------------------
        private void HandleDayStart()
        {
            if (currentScenario == null || GameManager.Instance == null) return;

            int day = GameManager.Instance.CurrentDay;
            var storyEvent = currentScenario.GetEventForDay(day);
            if (storyEvent != null)
            {
                TriggerEvent(storyEvent);
            }
            else
            {
                Debug.Log($"[Storyteller] Day {day}: No fixed event scheduled.");
                // TODO: Trigger Random Event or AI Generation here
            }
        }

        public void TriggerEvent(StoryEventSO storyEvent)
        {
            Debug.Log($"[Storyteller] Triggering Event: {storyEvent.Title}");
            
            // Execute Immediate Effects
            if (storyEvent.ImmediateEffects != null)
            {
                foreach (var effect in storyEvent.ImmediateEffects)
                {
                    effect.Execute();
                }
            }

            // TODO: Display UI for Event and Choices
            // For now, auto-select first choice if exists (simulation mode)
            // or just log it.
        }

        // -------------------------------------------------------------------------
        // Debug
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Button]
        public void TestTriggerEvent(StoryEventSO evt)
        {
            if (evt != null) TriggerEvent(evt);
        }

        [Button("Show Today's Event", ButtonSizes.Large)]
        [GUIColor(1f, 0.8f, 0.2f)]
        private void Debug_ShowTodayEvent()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("GameManager not found.");
                return;
            }
            
            if (currentScenario == null)
            {
                Debug.LogWarning("No Scenario assigned.");
                return;
            }

            int day = GameManager.Instance.CurrentDay;
            var evt = currentScenario.GetEventForDay(day);
            
            if (evt != null)
            {
                Debug.Log($"[Storyteller] Day {day}: {evt.Title} - {evt.Description}");
            }
            else
            {
                Debug.Log($"[Storyteller] Day {day}: Event doesn't exist.");
            }
        }
        #endif
    }
}
