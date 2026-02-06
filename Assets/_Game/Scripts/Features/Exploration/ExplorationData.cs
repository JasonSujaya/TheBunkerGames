using System;
using System.Collections.Generic;

namespace TheBunkerGames
{
    /// <summary>
    /// Defines a location that can be explored in the wasteland.
    /// Can be AI-generated or predefined via ScriptableObject.
    /// </summary>
    [Serializable]
    public class ExplorationLocation
    {
        public string LocationName = "Unknown";
        public string Description = "";
        public ExplorationRisk Risk = ExplorationRisk.Medium;
        public bool IsAvailable = true;
    }

    /// <summary>
    /// An active expedition: one character heading to one location.
    /// </summary>
    [Serializable]
    public class Expedition
    {
        public string ExplorerName;
        public ExplorationLocation Location;
        public bool IsComplete;
        public ExplorationResult Result;
    }

    /// <summary>
    /// The outcome of a single expedition.
    /// Contains loot, stat changes, injury status, and a narrative log.
    /// </summary>
    [Serializable]
    public class ExplorationResult
    {
        public string ExplorerName;
        public string LocationName;
        public List<ResourceGrantData> FoundItems = new List<ResourceGrantData>();
        public float HealthChange;
        public float SanityChange;
        public bool IsInjured;
        public string NarrativeLog = "";
    }
}
