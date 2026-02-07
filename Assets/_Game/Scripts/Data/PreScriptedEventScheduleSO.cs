using UnityEngine;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// ScriptableObject holding all pre-scripted story events for the 30-day survival game.
    /// These are hand-authored narrative beats that fire on specific days, separate from
    /// AI-generated story events.
    /// </summary>
    [CreateAssetMenu(fileName = "PreScriptedEventSchedule", menuName = "TheBunkerGames/Pre-Scripted Event Schedule")]
    public class PreScriptedEventScheduleSO : ScriptableObject
    {
        // -------------------------------------------------------------------------
        // Singleton Access
        // -------------------------------------------------------------------------
        private static PreScriptedEventScheduleSO instance;
        public static PreScriptedEventScheduleSO Instance => instance;

        public static void SetInstance(PreScriptedEventScheduleSO schedule)
        {
            instance = schedule;
        }

        // -------------------------------------------------------------------------
        // Event Schedule
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Pre-Scripted Event Schedule")]
        [InfoBox("Hand-authored narrative beats for each day. These fire alongside AI-generated events.")]
        [Searchable]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true)]
        #endif
        [SerializeField] private List<ScriptedEventEntry> events = new List<ScriptedEventEntry>();

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public List<ScriptedEventEntry> AllEvents => events;

        // -------------------------------------------------------------------------
        // Query Methods
        // -------------------------------------------------------------------------

        /// <summary>
        /// Get all pre-scripted events scheduled for a specific day.
        /// </summary>
        public List<ScriptedEventEntry> GetEventsForDay(int day)
        {
            List<ScriptedEventEntry> result = new List<ScriptedEventEntry>();
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i] != null && events[i].Day == day)
                    result.Add(events[i]);
            }
            return result;
        }

        /// <summary>
        /// Get all pre-scripted events of a specific category.
        /// </summary>
        public List<ScriptedEventEntry> GetEventsByCategory(ScriptedEventCategory category)
        {
            List<ScriptedEventEntry> result = new List<ScriptedEventEntry>();
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i] != null && events[i].Category == category)
                    result.Add(events[i]);
            }
            return result;
        }

        /// <summary>
        /// Check if a specific day has any pre-scripted events.
        /// </summary>
        public bool HasEventsForDay(int day)
        {
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i] != null && events[i].Day == day)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Get all pre-scripted events converted to LLMStoryEventData for processing
        /// through StorytellerManager.
        /// </summary>
        public List<LLMStoryEventData> GetStoryEventsForDay(int day)
        {
            List<LLMStoryEventData> result = new List<LLMStoryEventData>();
            var dayEvents = GetEventsForDay(day);
            for (int i = 0; i < dayEvents.Count; i++)
            {
                result.Add(dayEvents[i].ToStoryEventData());
            }
            return result;
        }

        // -------------------------------------------------------------------------
        // Debug
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug")]
        [Button("Log All Events", ButtonSizes.Medium)]
        private void Debug_LogAllEvents()
        {
            Debug.Log($"[PreScriptedEventSchedule] Total events: {events.Count}");
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i] != null)
                    Debug.Log($"  Day {events[i].Day}: {events[i].Title} [{events[i].Category}]");
            }
        }

        [Button("Log By Day", ButtonSizes.Medium)]
        private void Debug_LogByDay()
        {
            for (int day = 1; day <= 30; day++)
            {
                var dayEvents = GetEventsForDay(day);
                if (dayEvents.Count > 0)
                {
                    Debug.Log($"  Day {day}: {dayEvents.Count} event(s)");
                    for (int i = 0; i < dayEvents.Count; i++)
                        Debug.Log($"    - {dayEvents[i].Title} [{dayEvents[i].Category}]");
                }
            }
        }

        [Button("Validate Schedule", ButtonSizes.Large)]
        [GUIColor(0, 1, 0)]
        private void Debug_ValidateSchedule()
        {
            int issues = 0;
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i] == null) { issues++; continue; }
                if (string.IsNullOrEmpty(events[i].Title))
                {
                    Debug.LogWarning($"[PreScriptedEventSchedule] Entry {i} (Day {events[i].Day}) has no title.");
                    issues++;
                }
                if (string.IsNullOrEmpty(events[i].Description))
                {
                    Debug.LogWarning($"[PreScriptedEventSchedule] \"{events[i].Title}\" has no description.");
                    issues++;
                }
            }
            if (issues == 0)
                Debug.Log($"[PreScriptedEventSchedule] All {events.Count} events valid.");
            else
                Debug.LogWarning($"[PreScriptedEventSchedule] {issues} issue(s) found.");
        }
        #endif
    }
}
