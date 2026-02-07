using System;
using System.Collections.Generic;

namespace TheBunkerGames
{
    /// <summary>
    /// Wraps the LLM response for a single player action category.
    /// Contains the parsed story event, player input, and items used.
    /// </summary>
    [Serializable]
    public class PlayerActionResult
    {
        public PlayerActionCategory Category;
        public string PlayerInput;
        public List<string> ItemsUsed = new List<string>();
        public LLMStoryEventData StoryEvent;
        public bool IsComplete;
        public string Error;

        public bool HasError => !string.IsNullOrEmpty(Error);

        public override string ToString()
        {
            if (HasError) return $"[{Category}] ERROR: {Error}";
            if (StoryEvent != null) return $"[{Category}] {StoryEvent.Title}";
            return $"[{Category}] Pending...";
        }
    }
}
