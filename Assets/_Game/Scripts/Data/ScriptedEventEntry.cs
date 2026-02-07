using UnityEngine;
using System;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// A single hand-authored story event for the pre-scripted event schedule.
    /// Designers create these to inject planned narrative beats on specific days.
    /// </summary>
    [Serializable]
    public class ScriptedEventEntry
    {
        // -------------------------------------------------------------------------
        // Scheduling
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [FoldoutGroup("$Title")]
        [HorizontalGroup("$Title/DayRow", Width = 120)]
        [LabelText("Day")]
        #endif
        [SerializeField, Range(1, 30)] private int day = 1;

        #if ODIN_INSPECTOR
        [HorizontalGroup("$Title/DayRow")]
        [LabelText("Category")]
        #endif
        [SerializeField] private ScriptedEventCategory category;

        // -------------------------------------------------------------------------
        // Event Content
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [FoldoutGroup("$Title")]
        [GUIColor(0.4f, 1f, 0.4f)]
        #endif
        [SerializeField] private string title = "New Event";

        #if ODIN_INSPECTOR
        [FoldoutGroup("$Title")]
        [TextArea(3, 6)]
        #endif
        [SerializeField] private string description = "";

        // -------------------------------------------------------------------------
        // Optional Effects
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [FoldoutGroup("$Title/Effects")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        #endif
        [SerializeField] private List<LLMStoryEffectData> effects = new List<LLMStoryEffectData>();

        // -------------------------------------------------------------------------
        // Optional Choices
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [FoldoutGroup("$Title/Choices")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        #endif
        [SerializeField] private List<LLMStoryChoice> choices = new List<LLMStoryChoice>();

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public int Day => day;
        public ScriptedEventCategory Category => category;
        public string Title => title;
        public string Description => description;
        public List<LLMStoryEffectData> Effects => effects;
        public List<LLMStoryChoice> Choices => choices;

        // -------------------------------------------------------------------------
        // Conversion
        // -------------------------------------------------------------------------
        /// <summary>
        /// Convert to LLMStoryEventData for processing through the existing
        /// StorytellerManager pipeline.
        /// </summary>
        public LLMStoryEventData ToStoryEventData()
        {
            return new LLMStoryEventData
            {
                Title = title,
                Description = description,
                Effects = effects != null ? new List<LLMStoryEffectData>(effects) : new List<LLMStoryEffectData>(),
                Choices = choices != null ? new List<LLMStoryChoice>(choices) : new List<LLMStoryChoice>()
            };
        }

        public override string ToString() => $"Day {day}: {title} [{category}]";
    }
}
