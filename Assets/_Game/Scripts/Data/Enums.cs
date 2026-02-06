using UnityEngine;

namespace TheBunkerGames
{
    // -------------------------------------------------------------------------
    // Game State Machine States (matches the 5-phase Core Loop)
    // -------------------------------------------------------------------------
    public enum GameState
    {
        StatusReview,
        AngelInteraction,
        CityExploration,
        DailyChoice,
        NightCycle
    }

    // -------------------------------------------------------------------------
    // Item Types
    // -------------------------------------------------------------------------
    public enum ItemType
    {
        Food,
        Water,
        Meds,
        Tools,
        Junk
    }

    // -------------------------------------------------------------------------
    // Exploration Risk Levels
    // -------------------------------------------------------------------------
    public enum ExplorationRisk
    {
        Low,
        Medium,
        High,
        Deadly
    }

    // -------------------------------------------------------------------------
    // Choice Outcome Types
    // -------------------------------------------------------------------------
    public enum ChoiceOutcome
    {
        Positive,
        Negative,
        Mixed,
        Catastrophic
    }

    // -------------------------------------------------------------------------
    // Angel Mood (affects cooperation level)
    // -------------------------------------------------------------------------
    public enum AngelMood
    {
        Cooperative,
        Neutral,
        Mocking,
        Cold,
        Hostile,
        Glitching
    }

    // -------------------------------------------------------------------------
    // QuestData States (String constants for easy AI integration)
    // -------------------------------------------------------------------------
    public static class QuestState
    {
        public const string Active = "ACTIVE";
        public const string Completed = "COMPLETED";
        public const string Failed = "FAILED";
    }

    // -------------------------------------------------------------------------
    // Character Roles
    // -------------------------------------------------------------------------
    public enum CharacterRole
    {
        Father,
        Mother,
        Son,
        Daughter,
        Other
    }
}
