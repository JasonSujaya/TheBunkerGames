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
        private void HandleDayStart(int day)
        {
            if (currentScenario == null) return;

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
        #endif
    }
}
