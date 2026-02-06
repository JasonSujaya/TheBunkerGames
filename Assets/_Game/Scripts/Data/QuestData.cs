using UnityEngine;
using System;

namespace TheBunkerGames
{
    /// <summary>
    /// Plain C# class for quest tracking.
    /// AI sends commands to add/update quests using string IDs.
    /// </summary>
    [Serializable]
    public class QuestData
    {
        // -------------------------------------------------------------------------
        // QuestData Data
        // -------------------------------------------------------------------------
        /// <summary>
        /// Unique identifier (e.g., "FindWater", "FixFilter")
        /// </summary>
        public string Id;

        /// <summary>
        /// Display text shown to the player
        /// </summary>
        public string Description;

        /// <summary>
        /// Current state: "ACTIVE", "COMPLETED", or "FAILED"
        /// Use QuestState constants for safety.
        /// </summary>
        public string State;

        // -------------------------------------------------------------------------
        // Constructors
        // -------------------------------------------------------------------------
        public QuestData()
        {
            Id = string.Empty;
            Description = string.Empty;
            State = QuestState.Active;
        }

        public QuestData(string id, string description, string state = null)
        {
            Id = id;
            Description = description;
            State = state ?? QuestState.Active;
        }

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        public void SetState(string newState)
        {
            State = newState;
        }

        public bool IsActive => State == QuestState.Active;
        public bool IsCompleted => State == QuestState.Completed;
        public bool IsFailed => State == QuestState.Failed;
    }
}
