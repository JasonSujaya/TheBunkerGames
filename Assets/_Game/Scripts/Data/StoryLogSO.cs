using UnityEngine;
using System;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// ScriptableObject that stores the full AI-generated story log.
    /// Organized by day, each day contains a list of story events with
    /// the player action that triggered them and the full LLM response data.
    /// Persists in the editor as an .asset file so you can review past runs.
    /// </summary>
    [CreateAssetMenu(fileName = "StoryLog", menuName = "TheBunkerGames/Story Log")]
    public class StoryLogSO : ScriptableObject
    {
        // -------------------------------------------------------------------------
        // Data
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Story Log")]
        [InfoBox("Full AI-generated story log organized by day. Persists between play sessions.")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = false)]
        #endif
        [SerializeField] private List<StoryDay> days = new List<StoryDay>();

        #if ODIN_INSPECTOR
        [Title("Session Info")]
        [ReadOnly]
        #endif
        [SerializeField] private int totalEvents;
        [SerializeField] private string lastUpdated;

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------
        public List<StoryDay> Days => days;
        public int TotalEvents => totalEvents;

        /// <summary>
        /// Record a story event for a specific day.
        /// </summary>
        public void RecordEvent(int day, string playerAction, LLMStoryEventData storyEvent)
        {
            RecordEvent(day, playerAction, storyEvent, "");
        }

        /// <summary>
        /// Record a story event for a specific day with an optional category tag.
        /// </summary>
        public void RecordEvent(int day, string playerAction, LLMStoryEventData storyEvent, string category)
        {
            if (storyEvent == null) return;

            // Find or create the day entry
            var storyDay = days.Find(d => d.DayNumber == day);
            if (storyDay == null)
            {
                storyDay = new StoryDay { DayNumber = day };
                // Insert in order
                int insertIndex = days.FindIndex(d => d.DayNumber > day);
                if (insertIndex < 0)
                    days.Add(storyDay);
                else
                    days.Insert(insertIndex, storyDay);
            }

            // Add the event
            storyDay.Events.Add(new StoryLogEntry
            {
                PlayerAction = playerAction,
                Title = storyEvent.Title,
                Description = storyEvent.Description,
                FullEventJson = storyEvent.ToJson(true),
                EffectCount = storyEvent.Effects?.Count ?? 0,
                ChoiceCount = storyEvent.Choices?.Count ?? 0,
                Category = category,
                Timestamp = DateTime.Now.ToString("HH:mm:ss")
            });

            totalEvents++;
            lastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            MarkDirty();
        }

        /// <summary>
        /// Clear all story data for a fresh run.
        /// </summary>
        public void Clear()
        {
            days.Clear();
            totalEvents = 0;
            lastUpdated = "";
            MarkDirty();
        }

        /// <summary>
        /// Get all events for a specific day.
        /// </summary>
        public StoryDay GetDay(int dayNumber)
        {
            return days.Find(d => d.DayNumber == dayNumber);
        }

        private void MarkDirty()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }

    // -------------------------------------------------------------------------
    // Data Classes
    // -------------------------------------------------------------------------

    /// <summary>
    /// All events that happened on a single day.
    /// </summary>
    [Serializable]
    public class StoryDay
    {
        #if ODIN_INSPECTOR
        [Title("Day $DayNumber")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = false)]
        #endif
        public int DayNumber;
        public List<StoryLogEntry> Events = new List<StoryLogEntry>();

        public override string ToString() => $"Day {DayNumber} ({Events.Count} events)";
    }

    /// <summary>
    /// A single story event entry in the log.
    /// </summary>
    [Serializable]
    public class StoryLogEntry
    {
        #if ODIN_INSPECTOR
        [FoldoutGroup("$Title")]
        [LabelText("Player Did")]
        #endif
        public string PlayerAction;

        #if ODIN_INSPECTOR
        [FoldoutGroup("$Title")]
        [GUIColor(0.4f, 1f, 0.4f)]
        #endif
        public string Title;

        #if ODIN_INSPECTOR
        [FoldoutGroup("$Title")]
        [TextArea(2, 5)]
        #endif
        public string Description;

        #if ODIN_INSPECTOR
        [FoldoutGroup("$Title")]
        [HorizontalGroup("$Title/Counts")]
        #endif
        public int EffectCount;

        #if ODIN_INSPECTOR
        [HorizontalGroup("$Title/Counts")]
        #endif
        public int ChoiceCount;

        #if ODIN_INSPECTOR
        [FoldoutGroup("$Title")]
        #endif
        public string Category;

        #if ODIN_INSPECTOR
        [FoldoutGroup("$Title")]
        #endif
        public string Timestamp;

        #if ODIN_INSPECTOR
        [FoldoutGroup("$Title/Raw JSON")]
        [TextArea(5, 20)]
        #endif
        public string FullEventJson;

        public override string ToString() => Title;
    }
}
