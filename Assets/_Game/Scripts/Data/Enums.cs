using UnityEngine;

namespace TheBunkerGames
{
    // -------------------------------------------------------------------------
    // Game State Machine States
    // -------------------------------------------------------------------------
    public enum GameState
    {
        Morning,
        Scavenge,
        Voting,
        NightProcessing
    }

    // -------------------------------------------------------------------------
    // Item Types
    // -------------------------------------------------------------------------
    public enum ItemType
    {
        Food,
        Meds,
        Junk
    }

    // -------------------------------------------------------------------------
    // Quest States (String constants for easy AI integration)
    // -------------------------------------------------------------------------
    public static class QuestState
    {
        public const string Active = "ACTIVE";
        public const string Completed = "COMPLETED";
        public const string Failed = "FAILED";
    }
}
